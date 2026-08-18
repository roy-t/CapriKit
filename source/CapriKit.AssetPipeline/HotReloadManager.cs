using CapriKit.IO;
using CapriKit.IO.Watchers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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

            // The asset manager materializes an asset once per outstanding handle, so the same asset arrives
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
