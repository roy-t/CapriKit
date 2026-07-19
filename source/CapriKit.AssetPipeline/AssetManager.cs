using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;

    // Values are IRegisteredTranscoder<TAsset> instances, keyed by typeof(TAsset)
    private readonly Dictionary<Type, object> Transcoders = [];

    public AssetManager(DirectoryPath rootDirectory)
    {
        FileSystem = new FileSystem().ScopedTo(rootDirectory);
    }

    public AssetManager(IVirtualFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public void RegisterTranscoder<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder)
        where TSettings : IAssetSettings<TAsset>
    {
        Transcoders[typeof(TAsset)] = new RegisteredTranscoder<TAsset, TSettings>(transcoder);
    }

    public Task Encode<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
    {
        return GetTranscoder<TAsset>().Encode(id, settings, FileSystem);
    }

    public Task Encode<TAsset>(AssetId id)
    {
        return Encode(id, default(NoSettings<TAsset>));
    }

    public async Task<TAsset> Decode<TAsset>(AssetId id)
    {
        var asset = await GetTranscoder<TAsset>().Decode(id, FileSystem);
        return asset.Value;
    }

    private IRegisteredTranscoder<TAsset> GetTranscoder<TAsset>()
    {
        if (!Transcoders.TryGetValue(typeof(TAsset), out var transcoder))
        {
            throw new InvalidOperationException($"No transcoder registered for asset type {typeof(TAsset).Name}");
        }

        return (IRegisteredTranscoder<TAsset>)transcoder;
    }

    private interface IRegisteredTranscoder<TAsset>
    {
        Task Encode(AssetId id, IAssetSettings<TAsset> settings, IVirtualFileSystem fileSystem);
        Task<Asset<TAsset>> Decode(AssetId id, IVirtualFileSystem fileSystem);
    }

    // Bridges the public API, which only knows IAssetSettings<TAsset>, to the transcoder's
    // concrete TSettings. This lets callers omit TSettings, C# cannot infer it: type inference
    // never flows through generic constraints, only through parameter types.
    private sealed class RegisteredTranscoder<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder)
        : IRegisteredTranscoder<TAsset>
        where TSettings : IAssetSettings<TAsset>
    {
        public Task Encode(AssetId id, IAssetSettings<TAsset> settings, IVirtualFileSystem fileSystem)
        {
            if (settings is not TSettings typedSettings)
            {
                throw new ArgumentException(
                    $"Transcoder for {typeof(TAsset).Name} expects settings of type {typeof(TSettings).Name} but got {settings.GetType().Name}",
                    nameof(settings));
            }

            return AssetEncoder.Encode(id, typedSettings, transcoder, fileSystem);
        }

        public Task<Asset<TAsset>> Decode(AssetId id, IVirtualFileSystem fileSystem)
        {
            return AssetDecoder.Decode(id, transcoder, fileSystem);
        }
    }
}
