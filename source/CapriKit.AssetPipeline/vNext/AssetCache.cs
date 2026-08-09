using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.vNext;

/// <summary>
/// Cache of live assets. Methods are thread-safe and can be accessed concurrently. However,
/// cleaning-up unused asset using the <see cref="Collect"/> must be done from the main thread.
/// </summary>
internal sealed partial class AssetCache : IDisposable
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
    /// Stores the given asset and then leases it. If another caller already stored the asset
    /// the <paramref name="candidate"/> is disposed and the stored instance is leased instead.
    /// Thread-safe: loading the same asset twice is wasteful but harmless, after calling this
    /// method users must stop referencing <paramref name="candidate"/>.
    /// </summary>
    public TAsset PutOrLease<TAsset>(AssetId id, TAsset candidate)
        where TAsset : class
    {
        lock (Lock)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (Entries.TryGetValue(id, out var entry))
            {
                PendingDispose.Enqueue(new Entry(id, candidate, 0));

                var asset = Cast<TAsset>(entry, id);
                entry.RefCount++;
                return asset;
            }

            Entries.Add(id, new Entry(id, candidate, 1));
            return candidate;
        }
    }

    /// <summary>
    /// Attempts to retrieve the asset with the given id. Thread-safe.
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
    /// Returns a leased asset. If every user returned their asset it become collectable. Which happens in
    /// <see cref="Collect"/>. After calling return the caller must no longer reference the asset instance.
    /// Thread-safe.
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
    /// Disposes all assets that no longer have users. This method must be called from the primary thread,
    /// but it is safe for threads to concurrently access other methods in this class.
    /// </summary>
    public void Collect()
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
