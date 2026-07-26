namespace CapriKit.AssetPipeline.HotReloading;

/// <summary>
/// Represents an asset that is ready to be hot swapped by the main thread
/// </summary>
internal abstract class HotSwappable(AssetId id)
{
    public AssetId Id { get; } = id;
    public abstract void HotSwap(AssetManager manager, HotSwapManager hotSwapManager);
}

internal sealed class HotSwappable<TAsset>(TAsset instance, AssetJob<TAsset> newParts, IAssetSettings<TAsset> settings)
    : HotSwappable(newParts.Id)
    where TAsset : class
{
    public override void HotSwap(AssetManager assetManager, HotSwapManager hotSwapManager)
    {
        if (newParts.OnSuccess(out var asset))
        {
            assetManager.HotSwap(instance, asset.Value);

            // Instance keeps being the active object, so keep tracking instance, but with the new dependencies
            var toTrack = new Asset<TAsset>(asset.Id, instance, settings, asset.Dependencies);
            hotSwapManager.Track(toTrack, settings);
        }

        if (newParts.OnFailure(out var ex))
        {
            ex.Throw();
        }

        throw new Exception($"Asset {Id} tracked for hot swapping could no longer be found");
    }
}
