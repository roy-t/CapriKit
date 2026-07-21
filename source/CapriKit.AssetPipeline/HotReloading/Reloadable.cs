using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline.HotReloading;

/// <summary>
/// Represents an asset that can be rebuild and reloaded on demand.
/// </summary>
internal abstract class Reloadable
{
    protected volatile bool isReloading;

    public abstract AssetId Id { get; }
    public abstract Task Reload(AssetManager manager, ConcurrentQueue<HotSwappable> queue);

    public bool IsReloading => isReloading;
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

    public override async Task Reload(AssetManager manager, ConcurrentQueue<HotSwappable> queue)
    {
        try
        {
            isReloading = true;
            if (!Instance.TryGetTarget(out var cold)) { return; }

            // TODO: technically cold can be alive but disposed here

            await manager.Encode(Id, Settings);
            var hot = await manager.DecodeInternal<TAsset>(Id);
            queue.Enqueue(new HotSwappable<TAsset>(cold, hot, Settings));
        }
        finally
        {
            isReloading = false;
        }
    }
}
