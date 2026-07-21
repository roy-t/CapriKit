namespace CapriKit.AssetPipeline.HotReloading;

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
