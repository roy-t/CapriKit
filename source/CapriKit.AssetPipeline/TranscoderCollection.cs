namespace CapriKit.AssetPipeline;

internal sealed class TranscoderCollection
{
    private readonly Dictionary<Type, IAssetTranscoder> Transcoders = [];

    public void Register<TAsset>(IAssetTranscoder<TAsset> transcoder)
    {
        Transcoders[typeof(TAsset)] = transcoder;
    }

    public IAssetTranscoder<TAsset> Get<TAsset>()
    {
        if (!Transcoders.TryGetValue(typeof(TAsset), out var transcoder))
        {
            throw new InvalidOperationException($"No transcoder registered for asset type {typeof(TAsset).Name}");
        }

        return (IAssetTranscoder<TAsset>)transcoder;
    }
}
