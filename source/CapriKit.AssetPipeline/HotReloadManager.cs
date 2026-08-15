using CapriKit.Concurrency.Async;
using CapriKit.IO;
using CapriKit.IO.Watchers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Facilitates hot reloading and hot swapping of assets. Tracks the files used to create an asset
/// and triggers a rebuild on file changes. Takes care of threading and only performs the final
/// hot swap when <see cref="Update"/> is called.
/// </summary>
internal sealed partial class HotReloadManager : IDisposable
{
    private static readonly TimeSpan MinWaitTime = TimeSpan.FromSeconds(0.5);
    private readonly ILogger<HotReloadManager> Logger;

    private readonly ScopedFileSystem FileSystem;
    private readonly IVirtualFileSystemWatcher Watcher;
    private readonly FileSystemEventQueue FileChances;

    private readonly Lock TrackingLock;
    private readonly Dictionary<AssetId, List<HotReloadable>> Tracked;
    private readonly Dictionary<FilePath, HashSet<AssetId>> Dependents;

    private readonly HashSet<AssetId> PendingRebuilds;
    private readonly ConcurrentQueue<HotSwapAction> PendingReloads;

    private long lastFileChange;
    private volatile bool isReloading;

    public HotReloadManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<HotReloadManager>();

        FileSystem = fileSystem;
        Watcher = fileSystem.Watch();
        FileChances = new(Watcher);

        TrackingLock = new();
        Tracked = [];
        Dependents = [];

        PendingRebuilds = [];
        PendingReloads = [];

        lastFileChange = Stopwatch.GetTimestamp();
        isReloading = false;
    }

    /// <summary>
    /// Registers an asset for tracking by the hot-reload system.
    /// Threading: thread-safe, multiple threads can call this method and can even register
    /// the same asset multiple times. This class figures out which instances are still relevant.
    /// </summary>
    public void Track<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // An asset can be registered multiple times
        lock (TrackingLock)
        {
            var reloadable = new HotReloadable<TAsset, TSettings>(asset, transcoder);
            if (Tracked.TryGetValue(asset.Id, out var assets))
            {
                // TODO: this is broken!!!!
                // Prevent adding the exact same instance multiple times
                if (assets.Any(a => object.ReferenceEquals(a, asset))) { return; }
                assets.Add(reloadable);
            }
            else
            {
                Tracked[asset.Id] = [reloadable];
            }

            RegisterFileDependencies(asset.Id, asset.BuildMetaData.Dependencies);
        }
    }

    /// <summary>
    /// Stops tracking an asset
    /// </summary>
    public void UnTrack(AssetId id)
    {
        // Do not remove all references from dependents as that would require us to go through the entire collection
        // but just forgetting it from tracked the asset will no longer be reloaded.
        lock (TrackingLock)
        {
            Tracked.Remove(id);
        }
    }

    /// <summary>
    /// Checks which files required reloading, start the reloading process and hot swaps any asset that have reloaded
    /// Threading: Unsafe, must only be called by the primary thread. Other threads can call other methods in this class.
    /// </summary>
    public void Update()
    {
        if (isReloading)
        {
            return;
        }

        DrainFileChanges();
        var elapsed = Stopwatch.GetElapsedTime(lastFileChange);
        if (elapsed > MinWaitTime)
        {
            ReloadOne();
        }

        HotSwapPending();
    }

    /// <summary>
    /// Drains the queue of file events and adds any assets that dependent on this file to PendingRebuilds
    /// Threading: Unsafe, must be called single threaded because PendingRebuilds can only
    /// be used by one thread at a time.
    /// </summary>
    private void DrainFileChanges()
    {
        lock (TrackingLock)
        {
            while (FileChances.TryDequeue(out var @event))
            {
                if (Dependents.TryGetValue(@event.File, out var dependents))
                {
                    lastFileChange = Stopwatch.GetTimestamp();
                    foreach (var id in dependents)
                    {
                        PendingRebuilds.Add(id);
                        LogPendingReload(Logger, @event.File, id);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Starts rebuilding the first asset in the set
    /// Threading: Unsafe, must be called single threaded because the PendingRebuilds sets can only
    /// be used by one thread at a time and the isReloading guard would also be confused.
    /// </summary>
    private void ReloadOne()
    {
        if (PendingRebuilds.Count == 0) { return; }

        var id = PendingRebuilds.First();
        PendingRebuilds.Remove(id);

        HotReloadable? target = null;
        List<HotReloadable>? candidates;

        // Even though this method is single threaded, other methods that allow parallelism can
        // touch Tracked so we need to put a lock around it.
        lock (TrackingLock)
        {
            if (!Tracked.TryGetValue(id, out candidates))
            {
                return;
            }

            // TODO: we allow users to add the same asset-id multiple times but we expect
            // that (after a while) only one of the asset instances is still used by the engine. The
            // others will be garbage collected eventually. Still, at this point in time we cannot be sure
            // that the first (or last, or..) alive candidate is the one that will survive.
            // How should we deal with that?
            foreach (var candidate in candidates)
            {
                if (candidate.IsAlive)
                {
                    target = candidate;
                    break;
                }
            }
        }

        // Not finding a target is normal, it means a file
        // changed but the asset depending on it is no longer in use.
        if (target != null)
        {
            LogReloadStarted(Logger, id);

            isReloading = true;
            Task.Run(() => target.Reload(FileSystem, PendingReloads)
               .FireAndForget(ex =>
               {
                   LogReloadFailed(Logger, id, ex.SourceException);
                   isReloading = false;
               },
               () =>
               {
                   LogReloadCompleted(Logger, id);
                   isReloading = false;
               }));
        }
    }

    /// <summary>
    /// Hot swaps the assets that have been reloaded.
    /// Threading: Unsafe, the contract from <see cref="IAssetTranscoder{A,A}.HotSwap(A, A)"/> used here
    /// requires that assets are only hot swapped on the main thread
    /// </summary>
    private void HotSwapPending()
    {
        while (PendingReloads.TryDequeue(out var action))
        {
            try
            {
                LogHotSwapStarted(Logger, action.Id);
                action.PerformHotSwap();

                RegisterFileDependencies(action.Id, action.Dependencies);

                LogHotSwapCompleted(Logger, action.Id);
            }
            catch (Exception ex)
            {
                LogHotSwapFailed(Logger, action.Id, ex);
            }
        }
    }

    // Updates the dependents
    private void RegisterFileDependencies(AssetId id, IReadOnlyList<Dependency> dependencies)
    {
        lock (TrackingLock)
        {
            foreach (var dependency in dependencies)
            {
                var file = dependency.File;
                if (Dependents.TryGetValue(file, out var ids))
                {
                    ids.Add(id);
                }
                else
                {
                    ids = [id];
                    Dependents.Add(file, ids);
                }
            }
        }
    }

    public void Dispose()
    {
        Watcher.Stop();
        Tracked.Clear();
        Dependents.Clear();
        PendingRebuilds.Clear();
        PendingReloads.Clear();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Detected file change: {path}, affecting asset: {asset}")]
    private static partial void LogPendingReload(ILogger logger, FilePath path, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloading asset pending: {asset}")]
    private static partial void LogReloadStarted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloading asset completed: {asset}")]
    private static partial void LogReloadCompleted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reloading asset failed: {asset}")]
    private static partial void LogReloadFailed(ILogger logger, AssetId asset, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hot-swapping asset started: {asset}")]
    private static partial void LogHotSwapStarted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Hot-swapping asset completed: {asset}")]
    private static partial void LogHotSwapCompleted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Hot-swapping asset failed: {asset}")]
    private static partial void LogHotSwapFailed(ILogger logger, AssetId asset, Exception exception);
}
