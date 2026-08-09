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

    private readonly Dictionary<AssetId, HotReloadable> Tracked;
    private readonly Dictionary<FilePath, HashSet<AssetId>> Dependents;

    private readonly HashSet<AssetId> PendingRebuilds;
    private readonly ConcurrentQueue<HotSwapAction> PendingReloads;

    private long lastFileChange;
    private bool isReloading;

    public HotReloadManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<HotReloadManager>();

        FileSystem = fileSystem;
        Watcher = fileSystem.Watch();
        FileChances = new FileSystemEventQueue(Watcher);

        Tracked = [];
        Dependents = [];

        PendingRebuilds = [];
        PendingReloads = [];

        lastFileChange = Stopwatch.GetTimestamp();
        isReloading = false;
    }

    public void Track<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        Tracked[asset.Id] = new HotReloadable<TAsset, TSettings>(asset, transcoder);
        foreach (var dependency in asset.BuildMetaData.Dependencies)
        {
            var file = dependency.File;
            if (Dependents.TryGetValue(file, out var ids))
            {
                ids.Add(asset.Id);
            }
            else
            {
                ids = [asset.Id];
                Dependents.Add(file, ids);
            }
        }
    }

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

    private void DrainFileChanges()
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

    private void ReloadOne()
    {
        if (PendingRebuilds.Count > 0)
        {
            var id = PendingRebuilds.First();
            PendingRebuilds.Remove(id);

            isReloading = true;

            LogReloadStarted(Logger, id);

            var reloadable = Tracked[id];
            reloadable.Reload(FileSystem, PendingReloads)
                .FireAndForget(ex =>
                {
                    LogReloadFailed(Logger, id, ex);
                    isReloading = false;
                },
                () =>
                {
                    LogReloadCompleted(Logger, id);
                    isReloading = false;
                });
        }
    }

    private void HotSwapPending()
    {
        while (PendingReloads.TryDequeue(out var action))
        {
            try
            {
                LogHotSwapStarted(Logger, action.Id);
                action.PerformHotSwap();
                LogHotSwapCompleted(Logger, action.Id);
            }
            catch (Exception ex)
            {
                LogHotSwapFailed(Logger, action.Id, ex);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloading asset started: {asset}")]
    private static partial void LogReloadStarted(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Reloading asset completed: {asset}")]
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
