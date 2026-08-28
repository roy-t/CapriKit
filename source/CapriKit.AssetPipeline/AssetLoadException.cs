namespace CapriKit.AssetPipeline;

/// <summary>
/// Thrown from <see cref="AssetBundle{TContent}.IsReady"/> when building or loading an asset failed.
/// </summary>
public sealed class AssetLoadException(AssetId asset, Exception innerException)
    : Exception($"Failed to build or load asset: {asset}", innerException)
{
    /// <summary>
    /// The asset that could not be built or loaded.
    /// </summary>
    public AssetId Asset { get; } = asset;
}
