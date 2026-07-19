using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;

    // Values are IAssetTranscoder<TAsset> instances, keyed by typeof(TAsset)
    private readonly Dictionary<Type, IAssetTranscoder> Transcoders = [];

    public AssetManager(DirectoryPath rootDirectory)
    {
        FileSystem = new FileSystem().ScopedTo(rootDirectory);
    }

    public AssetManager(IVirtualFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public void RegisterTranscoder<TAsset>(IAssetTranscoder<TAsset> transcoder)
    {
        Transcoders[typeof(TAsset)] = transcoder;
    }

    public Task Encode<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
    {
        return AssetEncoder.Encode(id, settings, GetTranscoder<TAsset>(), FileSystem);
    }

    public Task Encode<TAsset>(AssetId id)
    {
        return Encode(id, default(NoSettings<TAsset>));
    }

    public async Task<TAsset> Decode<TAsset>(AssetId id)
    {
        var asset = await AssetDecoder.Decode(id, GetTranscoder<TAsset>(), FileSystem);
        return asset.Value;
    }

    private IAssetTranscoder<TAsset> GetTranscoder<TAsset>()
    {
        if (!Transcoders.TryGetValue(typeof(TAsset), out var transcoder))
        {
            throw new InvalidOperationException($"No transcoder registered for asset type {typeof(TAsset).Name}");
        }

        return (IAssetTranscoder<TAsset>)transcoder;
    }
}
