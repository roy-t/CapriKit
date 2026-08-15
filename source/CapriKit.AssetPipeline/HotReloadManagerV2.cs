using CapriKit.Concurrency.Async;
using CapriKit.IO;
using CapriKit.IO.Watchers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CapriKit.AssetPipeline;

internal record ReloadResult(AssetId Id, IReadOnlyList<Dependency> NewDependencies, Action HotSwap);

internal abstract record ReloadableV2
{
    public abstract Task Reload(ConcurrentQueue<ReloadResult> resultQueue);
}

internal record ReloadableV2<TAsset, TSettings>(AssetId Id, AssetBuildMetaData<TSettings> Metadata, IAssetTranscoder<TAsset, TSettings> Transcoder, IVirtualFileSystem FileSystem, AssetCache Cache)
    : ReloadableV2
    where TAsset : class
{
    public override async Task Reload(ConcurrentQueue<ReloadResult> resultQueue)
    {
        if (Cache.TryLease<TAsset>(Id, out var cold))
        {
            try
            {
                using var steam = new MemoryStream();
                // We store the encoded asset in memory instead of on disk to prevent
                // touching the file while other threads are also working on it.
                using var stream = new MemoryStream();
                await AssetEncoder.Encode(Id, Transcoder, Metadata.Settings, FileSystem, stream);

                stream.Seek(0, SeekOrigin.Begin);
                var hot = await AssetDecoder.Decode(Id, Transcoder, FileSystem, stream);

                resultQueue.Enqueue(new ReloadResult(Id, hot.BuildMetaData.Dependencies,
                    () =>
                    {
                        Transcoder.HotSwap(cold, hot.Value);
                        Cache.Return(Id);
                    }));
            }
            catch
            {
                Cache.Return(Id);
                throw;
            }
        }
    }
}

internal sealed partial class HotReloadManagerV2 : IDisposable
{
    private static readonly TimeSpan MinWaitTime = TimeSpan.FromSeconds(0.5);
    private readonly ILogger<HotReloadManager> Logger;
    private readonly AssetCache Cache;
    private readonly ScopedFileSystem FileSystem;
    private readonly IVirtualFileSystemWatcher Watcher;
    private readonly FileSystemEventQueue FileChanges;
    private readonly Dictionary<AssetId, ReloadableV2> Tracked;
    private readonly Dictionary<FilePath, HashSet<AssetId>> Dependents;

    private readonly HashSet<AssetId> PendingRebuilds;
    private readonly ConcurrentQueue<ReloadResult> PendingReloads;

    private readonly Lock Lock;

    private long lastFileChange;

    public HotReloadManagerV2(ILoggerFactory logger, AssetCache cache, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<HotReloadManager>();
        Cache = cache;
        FileSystem = fileSystem;
        Lock = new();
        Tracked = [];
        Dependents = [];
        PendingRebuilds = [];
        PendingReloads = [];

        Watcher = FileSystem.Watch();
        FileChanges = new(Watcher);
    }



    public void Track<TAsset, TSettings>(AssetId id, AssetBuildMetaData<TSettings> metadata, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        lock (Lock)
        {
            // Track each asset only once
            if (Tracked.TryGetValue(id, out var _)) { return; }
            Tracked[id] = new ReloadableV2<TAsset, TSettings>(id, metadata, transcoder, FileSystem, Cache);

            foreach (var dependency in metadata.Dependencies)
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

    public void Update()
    {
        DrainFileChanges();
        var elapsed = Stopwatch.GetElapsedTime(lastFileChange);
        if (elapsed > MinWaitTime)
        {
            Rebuild();
        }

        HotSwapPending();
    }


    /// <summary>
    /// Drains the queue of file events and adds any assets that dependent on this file to PendingRebuilds
    /// Threading: thread-safe
    /// </summary>
    private void DrainFileChanges()
    {
        lock (Lock)
        {
            while (FileChanges.TryDequeue(out var @event))
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
    /// Starts rebuilding the asset in the set
    /// Threading: thread-safe
    /// </summary>
    private void Rebuild()
    {
        lock (Lock)
        {
            foreach (var id in PendingRebuilds)
            {
                if (Tracked.TryGetValue(id, out var reloadable))
                {
                    Task.Run(() =>
                    {
                        LogReloadStarted(Logger, id);
                        reloadable.Reload(PendingReloads); // TODO: do I FireAndForget the outer, inner or both?
                        LogReloadCompleted(Logger, id);
                    }).FireAndForget(ex =>
                    {
                        LogReloadFailed(Logger, id, ex.SourceException);
                        // TODO: consider error scenarios, especially
                        // what happens to the lease?
                    });
                }
            }
            PendingRebuilds.Clear();
        }
    }

    /// <summary>
    /// Hot swaps the assets that have been reloaded.
    /// Threading: Unsafe, the contract from <see cref="IAssetTranscoder{A,A}.HotSwap(A, A)"/> used here
    /// requires that assets are only hot swapped on the main thread
    /// </summary>
    private void HotSwapPending()
    {
        while (PendingReloads.TryDequeue(out var reloadable))
        {
            try
            {
                LogHotSwapStarted(Logger, reloadable.Id);
                reloadable.HotSwap();

                RegisterFileDependencies(reloadable.Id, reloadable.NewDependencies);

                LogHotSwapCompleted(Logger, reloadable.Id);
            }
            catch (Exception ex)
            {
                LogHotSwapFailed(Logger, reloadable.Id, ex);
            }
        }
    }

    public void Dispose()
    {
        Watcher.Stop();
        // TODO: drain in-progress reloads
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
