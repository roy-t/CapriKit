using CapriKit.Concurrency.Async;
using CapriKit.IO;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline;

// TODO: fix all outstanding todos and simplify

internal abstract class Reloadable
{
    public abstract AssetId Id { get; }
    public abstract bool IsAlive { get; }
    public abstract Task Reload(AssetManager manager, HotSwapQueue queue);
}

internal sealed class Reloadable<TAsset> : Reloadable
    where TAsset : class
{
    private readonly WeakReference<TAsset> Instance;
    private readonly IAssetSettings<TAsset> Settings;

    public Reloadable(AssetId id, TAsset asset, IAssetSettings<TAsset> settings)
    {
        Instance = new WeakReference<TAsset>(asset);
        Settings = settings;
        Id = id;
    }

    public override AssetId Id { get; }
    public override bool IsAlive => Instance.TryGetTarget(out _);

    public override async Task Reload(AssetManager manager, HotSwapQueue queue)
    {
        if (!Instance.TryGetTarget(out var cold)) { return; }

        // TODO: technically cold can be alive but disposed here

        await manager.Encode(Id, Settings);
        var hot = await manager.Decode<TAsset>(Id);
        queue.Enqueue(cold, hot);
    }
}

internal sealed class HotSwapQueue
{
    private interface IHotSwappable
    {
        public void HotSwap(AssetManager manager);
    }

    private sealed class HotSwappable<TAsset>(TAsset cold, TAsset hot) : IHotSwappable
    {
        public void HotSwap(AssetManager manager)
        {
            manager.HotSwap(cold, hot);
        }
    }

    private readonly ConcurrentQueue<IHotSwappable> Queue = [];

    public void Enqueue<TAsset>(TAsset cold, TAsset hot)
    {
        Queue.Enqueue(new HotSwappable<TAsset>(cold, hot));
    }

    public void HotSwapPending(AssetManager manager)
    {
        while (Queue.TryDequeue(out var result))
        {
            result.HotSwap(manager);
        }
    }
}

internal sealed class HotSwapManager : IDisposable
{
    private readonly AssetManager AssetManager;
    private readonly IReadOnlyVirtualFileSystem FileSystem;
    private readonly FileSystemEventListener Listener;
    private readonly Dictionary<FilePath, List<Reloadable>> Dependents;
    private readonly ConcurrentQueue<FilePath> PendingFileChanges;
    private readonly HotSwapQueue HotSwapQueue;

    // TODO: add debounce logic by just refusing to update anything for X time after a change
    // so a single DateTime updated by ProcessUpdates while draining the queue

    public HotSwapManager(AssetManager assetManager, IReadOnlyVirtualFileSystem fileSystem)
    {
        Dependents = [];
        PendingFileChanges = [];
        HotSwapQueue = new HotSwapQueue();

        AssetManager = assetManager;
        FileSystem = fileSystem;

        // TODO: get the right directory to watch
        Listener = new FileSystemEventListener(""); 
        Listener.OnFileChanged += (sender, @event) =>
        {
            var (target, reason) = @event;
            PendingFileChanges.Enqueue(target);
        };
    }

    public void Track<TAsset>(AssetId id, Asset<TAsset> asset, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        var reloadable = new Reloadable<TAsset>(id, asset.Value, settings);
        foreach (var dependency in asset.Dependencies)
        {
            // TODO: file system events give the full path, but our dependencies might
            // have a relative path. We need to ensure both are the same or they will never match
            var file = dependency.File; 
            if (Dependents.TryGetValue(file, out var list))
            {
                list.Add(reloadable);
            }
            else
            {
                Dependents[file] = [reloadable];
            }
        }
    }

    public void ProcessUpdates()
    {
        // TODO: pending file changes needs to be debounced
        // and multiple file changes might link to one asset change
        // so we should first translate this to a hashset of AssetIds we want
        // to update, then wait for the debounce window to close, then update all
        // assets. 
        while (PendingFileChanges.TryDequeue(out var result))
        {
            if (Dependents.TryGetValue(result, out var dependents))
            {
                // TODO: It is also possible that re-encoding is still in progress while we get another
                // file change so we should not start more work before FireAndForget marks its task as completed
                // (see overload).

                // TODO: after updating we need to 'retrack' as the dependencies can have changed
                Update(dependents).FireAndForget(ex => { });
            }
        }

        HotSwapQueue.HotSwapPending(AssetManager);

        // TODO: remove all no-longer-alive items
    }

    private async Task Update(IReadOnlyList<Reloadable> targets)
    {
        foreach (var target in targets)
        {
            await target.Reload(AssetManager, HotSwapQueue);
        }
    }

    public void Dispose()
    {
        Listener.Dispose();
    }
}
