using CapriKit.IO;
using CapriKit.IO.Watchers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Rebuilds, reloads and hot-swaps tracked assets whenever one of the files they were built from changes.
/// Rebuilding and reloading happen on the thread pool, only the final hot-swap runs on the main thread
/// (see <see cref="IAssetTranscoder{TAsset, TSettings}.HotSwap"/>).
/// A failed rebuild, reload or hot-swap only means that the asset keeps its current contents, it never
/// invalidates a live asset and never leaves a lease behind in the <see cref="AssetPool"/>.
/// Threading: <see cref="Track"/> may be called from any thread, <see cref="Update"/> and
/// <see cref="Dispose"/> must be called from the main thread.
/// </summary>
internal sealed partial class HotReloadManager : IDisposable
{
    private readonly ILogger<HotReloadManager> Logger;
    private readonly AssetPool Cache;
    private readonly ScopedFileSystem FileSystem;
    private readonly IVirtualFileSystemWatcher Watcher;
    private readonly FileSystemEventQueue FileChanges;
    private readonly TimeSpan Debounce;

    // Guards the two collections that Track (any thread) and Update (main thread) share.
    private readonly Lock Lock;
    private readonly Dictionary<AssetId, TrackedAsset> Tracked;
    private readonly Dictionary<FilePath, HashSet<AssetId>> Dependents;

    // Only touched by the main thread.
    private readonly HashSet<AssetId> Stale;
    private readonly Dictionary<AssetId, Task<ReloadedAsset>> InFlight;
    private long lastChange;

    private bool isDisposed;

    /// <summary>
    /// The optional <c>debounce</c> is how long to wait after the last relevant file change before rebuilding.
    /// Editors write their buffer in several steps, without a pause we would rebuild the same asset once per
    /// step and read half-written files. If not provided, it will be set to 500 milliseconds.
    /// </summary>
    public HotReloadManager(ILoggerFactory loggerFactory, AssetPool cache, ScopedFileSystem fileSystem, TimeSpan? debounce = null)
    {
        Logger = loggerFactory.CreateLogger<HotReloadManager>();
        Cache = cache;
        FileSystem = fileSystem;
        Debounce = debounce ?? TimeSpan.FromSeconds(0.5);

        Lock = new();
        Tracked = [];
        Dependents = [];
        Stale = [];
        InFlight = [];

        Watcher = FileSystem.Watch();
        FileChanges = new FileSystemEventQueue(Watcher);
    }

    /// <summary>
    /// Registers an asset so that it is rebuilt, reloaded and hot-swapped whenever one of the files it was
    /// built from changes. Tracking the same asset more than once is a no-op.
    /// Threading: thread-safe, may be called from any thread at any time.
    /// </summary>
    public void Track<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        lock (Lock)
        {
            // After disposal we no longer listen for file changes, so tracking would only grow the maps.
            if (isDisposed) { return; }

            // The asset manager materializes an asset once per waiting requester, so the same asset arrives
            // here several times. Everything we store comes from the build, so the first registration wins.
            if (Tracked.ContainsKey(asset.Id)) { return; }

            var tracked = new TrackedAsset<TAsset, TSettings>(asset, transcoder);
            Tracked.Add(asset.Id, tracked);
            RegisterDependencies(tracked);
        }
    }

    /// <summary>
    /// Reacts to file changes, starts rebuilding the assets those files affect and hot-swaps the assets that
    /// finished rebuilding. Every step is bounded work, the expensive rebuilding and reloading happens on the
    /// thread pool so that the main thread only pays for the hot-swap itself.
    /// Threading: must only be called from the main thread.
    /// </summary>
    public void Update()
    {
        if (isDisposed) { return; }

        MarkStaleAssets();

        // Wait for the dust to settle so that a single save does not trigger a burst of rebuilds.
        if (Stopwatch.GetElapsedTime(lastChange) >= Debounce)
        {
            StartReloads();
        }

        FinishReloads();
    }

    /// <summary>
    /// Stops listening for file changes, abandons everything that has not started yet and finishes the
    /// rebuilds that are already running so that their leases and freshly built data are handed back.
    /// Threading: must only be called from the main thread.
    /// </summary>
    public void Dispose()
    {
        lock (Lock)
        {
            if (isDisposed) { return; }
            isDisposed = true;
        }

        Watcher.Stop();
        Stale.Clear();

        // The running rebuilds hold a lease and own freshly built data that only the main thread can dispose
        // of, so instead of abandoning them we wait and then finish them through the regular path.
        try
        {
            Task.WaitAll([.. InFlight.Values]);
        }
        catch (AggregateException)
        {
            // Failures are reported and cleaned up per asset by FinishReloads
        }

        FinishReloads();
    }

    /// <summary>
    /// Marks every asset that depends on a changed file as stale.
    /// </summary>
    private void MarkStaleAssets()
    {
        lock (Lock)
        {
            while (FileChanges.TryDequeue(out var change))
            {
                if (!Dependents.TryGetValue(change.File, out var dependents)) { continue; }

                // Only changes we actually care about restart the debounce window, otherwise unrelated
                // writes (such as the asset pipeline writing its own build files) could postpone a rebuild.
                lastChange = Stopwatch.GetTimestamp();

                foreach (var id in dependents)
                {
                    if (Stale.Add(id))
                    {
                        LogAssetStale(Logger, change.File, id);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Starts rebuilding and reloading every stale asset on the thread pool.
    /// </summary>
    private void StartReloads()
    {
        if (Stale.Count == 0) { return; }

        lock (Lock)
        {
            foreach (var id in Stale.ToArray())
            {
                // Let the running rebuild finish first. The asset stays stale so we pick it up again
                // afterwards, which is exactly what we want because its files changed once more.
                if (InFlight.ContainsKey(id)) { continue; }

                Stale.Remove(id);

                if (!Tracked.TryGetValue(id, out var tracked)) { continue; }

                if (tracked.TryStartReload(Cache, FileSystem, out var reload))
                {
                    InFlight.Add(id, reload);
                    LogReloadStarted(Logger, id);
                }
                else
                {
                    // The cache is the authority on liveness: no entry means nobody uses this asset anymore.
                    UntrackAsset(tracked);
                    LogUntracked(Logger, id);
                }
            }
        }
    }

    /// <summary>
    /// Hot-swaps every asset that finished rebuilding and returns the lease that its rebuild took.
    /// </summary>
    private void FinishReloads()
    {
        if (InFlight.Count == 0) { return; }

        foreach (var (id, reload) in InFlight.ToArray())
        {
            if (!reload.IsCompleted) { continue; }

            InFlight.Remove(id);
            FinishReload(id, reload);
        }
    }

    private void FinishReload(AssetId id, Task<ReloadedAsset> reload)
    {
        try
        {
            if (!reload.IsCompletedSuccessfully)
            {
                // Nothing was touched yet, so the asset simply keeps the contents it already had.
                LogReloadFailed(Logger, id, reload.Exception!);
                return;
            }

            var reloaded = reload.Result;

            // Deliberately outside of the lock: the transcoder runs code we do not control here.
            reloaded.HotSwap();

            UpdateDependencies(id, reloaded.Dependencies);
            LogHotSwapped(Logger, id);
        }
        catch (Exception ex)
        {
            // A transcoder that fails half-way leaves the asset in whatever state it made of it, all we can
            // do is report it. The alternative, throwing, would take down the game over a development feature.
            LogHotSwapFailed(Logger, id, ex);
        }
        finally
        {
            // Balances the lease that TryStartReload took, whether we managed to hot-swap or not.
            Cache.Return(id);
        }
    }

    /// <summary>
    /// Replaces the dependencies of an asset with the ones its latest build read.
    /// </summary>
    private void UpdateDependencies(AssetId id, IReadOnlyList<Dependency> dependencies)
    {
        lock (Lock)
        {
            if (Tracked.TryGetValue(id, out var tracked))
            {
                UnregisterDependencies(tracked);
                tracked.Dependencies = dependencies;
                RegisterDependencies(tracked);
            }
        }
    }

    // Threading: must be called while holding the lock
    private void RegisterDependencies(TrackedAsset tracked)
    {
        foreach (var (file, _) in tracked.Dependencies)
        {
            if (!Dependents.TryGetValue(file, out var ids))
            {
                ids = [];
                Dependents.Add(file, ids);
            }

            ids.Add(tracked.Id);
        }
    }

    // Threading: must be called while holding the lock
    private void UnregisterDependencies(TrackedAsset tracked)
    {
        foreach (var (file, _) in tracked.Dependencies)
        {
            if (Dependents.TryGetValue(file, out var ids) && ids.Remove(tracked.Id) && ids.Count == 0)
            {
                Dependents.Remove(file);
            }
        }
    }

    // Threading: must be called while holding the lock
    private void UntrackAsset(TrackedAsset tracked)
    {
        UnregisterDependencies(tracked);
        Tracked.Remove(tracked.Id);
    }


    /// <summary>
    /// A freshly rebuilt asset that is waiting for the main thread to move it into the live instance.
    /// </summary>
    /// <param name="Dependencies">Files read while rebuilding, used to keep the file-to-asset map up-to-date.</param>
    /// <param name="HotSwap">
    /// Moves the rebuilt data into the live instance. Threading: main thread only, see <see cref="IAssetTranscoder{TAsset, TSettings}.HotSwap"/>.
    /// </param>
    private sealed record ReloadedAsset(IReadOnlyList<Dependency> Dependencies, Action HotSwap);

    /// <summary>
    /// Everything the <see cref="HotReloadManager"/> needs to rebuild a single asset. The asset and settings
    /// types are erased so that assets of every type can live in one collection.
    /// </summary>
    private abstract class TrackedAsset(AssetId id, IReadOnlyList<Dependency> dependencies)
    {
        public AssetId Id { get; } = id;

        /// <summary>
        /// The files that the most recent successful build of this asset read.
        /// Threading: only touched by the main thread while it holds the manager's lock.
        /// </summary>
        public IReadOnlyList<Dependency> Dependencies { get; set; } = dependencies;

        /// <summary>
        /// Leases the live asset from the cache and then starts rebuilding it on the thread pool. Returns false if
        /// the asset is no longer in the cache, in which case no lease was taken and nothing was started.
        /// The lease is taken before the task starts so that the caller always knows whether a lease exists, the
        /// caller owns that lease and must return it once <paramref name="reload"/> has completed.
        /// Threading: main thread only.
        /// </summary>
        public abstract bool TryStartReload(AssetPool cache, IVirtualFileSystem fileSystem, [NotNullWhen(true)] out Task<ReloadedAsset>? reload);
    }

    /// <inheritdoc cref="TrackedAsset"/>
    private sealed class TrackedAsset<TAsset, TSettings> : TrackedAsset
        where TAsset : class
    {
        private readonly TSettings Settings;
        private readonly IAssetTranscoder<TAsset, TSettings> Transcoder;

        public TrackedAsset(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
            : base(asset.Id, asset.BuildMetaData.Dependencies)
        {
            Settings = asset.BuildMetaData.Settings;
            Transcoder = transcoder;
        }

        public override bool TryStartReload(AssetPool cache, IVirtualFileSystem fileSystem, [NotNullWhen(true)] out Task<ReloadedAsset>? reload)
        {
            // The lease pins the live instance for the entire rebuild, so the background thread never has to
            // wonder whether the object it is going to hot-swap into still exists.
            if (!cache.TryLease<TAsset>(Id, out var live))
            {
                reload = null;
                return false;
            }

            reload = Task.Run(() => Reload(live, fileSystem));
            return true;
        }

        private async Task<ReloadedAsset> Reload(TAsset live, IVirtualFileSystem fileSystem)
        {
            // Build into memory rather than over the existing build on disk: other threads may be reading that
            // file to load the very same asset and overwriting it underneath them would fail those loads.
            using var stream = new MemoryStream();
            await AssetEncoder.Encode(Id, Transcoder, Settings, fileSystem, stream);

            stream.Seek(0, SeekOrigin.Begin);
            var rebuilt = await AssetDecoder.Decode(Id, Transcoder, fileSystem, stream);

            return new ReloadedAsset(rebuilt.BuildMetaData.Dependencies, () => Transcoder.HotSwap(live, rebuilt.Value));
        }
    }


    [LoggerMessage(Level = LogLevel.Information, Message = "Detected change in file: {file}, marking asset: {asset} as stale")]
    private static partial void LogAssetStale(ILogger logger, FilePath file, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started rebuilding and reloading asset: {asset}")]
    private static partial void LogReloadStarted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Rebuilding or reloading asset: {asset} failed, it keeps its current contents")]
    private static partial void LogReloadFailed(ILogger logger, AssetId asset, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hot-swapped asset: {asset}")]
    private static partial void LogHotSwapped(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Hot-swapping asset: {asset} failed")]
    private static partial void LogHotSwapFailed(ILogger logger, AssetId asset, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped tracking asset: {asset}, it is no longer in the cache")]
    private static partial void LogUntracked(ILogger logger, AssetId asset);
}
