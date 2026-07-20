using CapriKit.IO;
using static CapriKit.AssetPipeline.AssetUtilities;

namespace CapriKit.AssetPipeline;

public sealed class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;

    // Values are IAssetTranscoder<TAsset> instances, keyed by typeof(TAsset)
    private readonly Dictionary<Type, IAssetTranscoder> Transcoders = [];
    private readonly AssetCache Cache;

    public AssetManager(DirectoryPath rootDirectory)
        : this(new FileSystem().ScopedTo(rootDirectory)) { }

    public AssetManager(IVirtualFileSystem fileSystem)
    {
        FileSystem = fileSystem;
        Cache = new AssetCache();
    }

    public void PushScope() => Cache.PushScope();
    public void PopScope() => Cache.PopScope();

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

    public async Task<TAsset> Load<TAsset>(AssetId id, IAssetSettings<TAsset> settings, bool rebuildOnFailure = true, bool rebuildOnOutOfDate = true)
    {
        // If a file was already loaded successfully we do not have to do an out-of-date check.
        // Keeping live assets up-to-date is handled by the hot-reloading machinery.
        if (Cache.TryGet<TAsset>(id, out var entry))
        {
            return entry.Value;
        }

        try
        {
            var asset = await AssetDecoder.Decode(id, GetTranscoder<TAsset>(), FileSystem);
            if (!IsUpToDate(asset, FileSystem) && rebuildOnOutOfDate)
            {
                (asset.Value as IDisposable)?.Dispose();
                await Encode(id, settings);
                return await Load(id, settings, false, false);
            }

            Cache.Add(id, asset);
            return asset.Value;
        }
        catch (Exception)
        {
            if (rebuildOnFailure)
            {
                await Encode(id, settings);
                return await Load(id, settings, false, false);
            }
            else
            {
                throw;
            }
        }
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
