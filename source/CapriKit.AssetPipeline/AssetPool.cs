using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Pool of keyed, reference-counted deduplicated live assets.
/// Most methods are thread-safe and can be accessed concurrently. However,
/// cleaning-up unused asset using the <see cref="DisposeReleased"/> must be done from the main thread.
/// </summary>
internal sealed partial class AssetPool : IDisposable
{
    private sealed class Entry(AssetId id, object asset, int refCount)
    {
        public AssetId Id { get; } = id;
        public object Asset { get; } = asset;
        public int RefCount { get; set; } = refCount;
    }

    private readonly Lock Lock = new();
    private readonly Dictionary<AssetId, Entry> Entries = [];
    private readonly Queue<Entry> PendingDispose = [];
    private bool isDisposed;

    /// <summary>
    /// Stores the given asset and then leases it. If an asset for the same asset id is added multiple
    /// times the first added asset wins. Assets added later just take a lease on the already
    /// added one and the candidate is disposed of.
    /// Threading: thread-safe, loading the same asset twice is wasteful but harmless, after calling this
    /// method users must stop referencing <paramref name="candidate"/> and use the return value of this method instead.
    /// </summary>
    public TAsset PutOrLease<TAsset>(AssetId id, TAsset candidate)
        where TAsset : class
    {
        lock (Lock)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (Entries.TryGetValue(id, out var entry))
            {
                // If someone adds the same asset twice we ensure that
                // the version already in the cache wins and update
                // the ref count accordingly
                if (!object.ReferenceEquals(candidate, entry.Asset))
                {
                    // If a different instance was added for the same id
                    // the candidate also needs to be disposed.
                    PendingDispose.Enqueue(new Entry(id, candidate, 0));
                }

                
                var asset = Cast<TAsset>(entry, id);
                entry.RefCount++;
                return asset;
            }

            Entries.Add(id, new Entry(id, candidate, 1));
            return candidate;
        }
    }

    /// <summary>
    /// Attempts to retrieve the asset with the given id.
    /// Threading: thread-safe.
    /// </summary>
    public bool TryLease<TAsset>(AssetId id, [NotNullWhen(true)] out TAsset? asset)
        where TAsset : class
    {
        lock (Lock)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (Entries.TryGetValue(id, out var entry))
            {
                asset = Cast<TAsset>(entry, id);
                entry.RefCount++;
                return true;
            }

            asset = default;
            return false;
        }
    }

    /// <summary>
    /// Returns a leased asset. If every user returned their asset it becomes collectable. Which happens in
    /// <see cref="DisposeReleased"/>. After calling return the caller must no longer reference the asset instance.
    /// Threading: thread-safe.
    /// </summary>
    public void Return(AssetId id)
    {
        lock (Lock)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (Entries.TryGetValue(id, out var entry))
            {
                entry.RefCount--;
                // Evict items immediately, but only dispose of them in Collect
                if (entry.RefCount <= 0)
                {
                    Entries.Remove(id);
                    PendingDispose.Enqueue(entry);
                }
            }
            else
            {
                throw new InvalidOperationException($"Returned {id} which was not found in the cache.");
            }
        }
    }

    /// <summary>
    /// Disposes all assets that no longer have users.
    /// Threading: this method must only be called from the primary thread.
    /// </summary>
    public void DisposeReleased()
    {
        List<Entry>? toDispose = null;

        // Collecting usually is a no-op and runs on the most important thread,so avoid waiting on acquiring the lock.
        if (Lock.TryEnter())
        {
            try
            {
                if (isDisposed) { return; }
                toDispose = DrainPendingDisposeQueue();
            }
            finally
            {
                Lock.Exit();
            }
        }

        DisposeDrainedItems(toDispose);
    }

    public void Dispose()
    {
        int leaked;
        List<Entry>? toDispose;

        lock (Lock)
        {
            if (isDisposed) { return; }
            isDisposed = true;

            leaked = Entries.Count;
            Entries.Clear();

            toDispose = DrainPendingDisposeQueue();
        }

        DisposeDrainedItems(toDispose);

        if (leaked > 0)
        {
            throw new Exception($"Cache will leak {leaked} entries that have not been returned before the cache was disposed.");
        }
    }

    // Must be called from inside a lock, returns null if there's nothing to dispose
    private List<Entry>? DrainPendingDisposeQueue()
    {
        List<Entry>? toDispose = null;
        while (PendingDispose.TryDequeue(out var entry))
        {
            (toDispose ??= []).Add(entry);
        }
        return toDispose;
    }

    // Must called from outside a lock. Dispose runs code outside of our control, might run long
    // and might even interact with the asset cache (in which case running it inside the lock
    // would cause deadlocks).
    private static void DisposeDrainedItems(List<Entry>? toDispose)
    {
        if (toDispose != null)
        {
            foreach (var entry in toDispose)
            {
                (entry.Asset as IDisposable)?.Dispose();
            }
        }
    }

    private static TAsset Cast<TAsset>(Entry entry, AssetId id)
        where TAsset : class
    {
        return entry.Asset as TAsset
            ?? throw new InvalidOperationException($"{id} is cached as {entry.Asset.GetType().Name} but was requested as {typeof(TAsset).Name}.");
    }
}
