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
        where TAsset : class
    {
        return Encode(id, default(NoSettings<TAsset>));
    }

    public async Task<TAsset> Decode<TAsset>(AssetId id)
        where TAsset : class
    {
        var asset = await DecodeInternal<TAsset>(id);
        return asset.Value;
    }

    internal Task<Asset<TAsset>> DecodeInternal<TAsset>(AssetId id)
        where TAsset : class
    {
        return AssetDecoder.Decode(id, GetTranscoder<TAsset>(), FileSystem);
    }

    /// <summary>
    /// Loads an asset from the cache, decoding it from disk and building it first if it is missing or
    /// out of date. Loaded assets are owned by the current scope, see <see cref="PushScope"/>.
    /// </summary>
    public async Task<TAsset> Load<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        // If an asset was already loaded successfully we do not have to do an out-of-date check.
        // Keeping live assets up-to-date is handled by the hot-reloading machinery.
        if (Cache.TryGet<TAsset>(id, out var entry))
        {
            return entry;
        }

        var asset = await DecodeOrBuild(id, settings);
        Cache.Add<TAsset>(id, asset.Value);
        return asset.Value;
    }

    public Task<TAsset> Load<TAsset>(AssetId id)
        where TAsset : class
    {
        return Load(id, default(NoSettings<TAsset>));
    }

    private async Task<Asset<TAsset>> DecodeOrBuild<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        var transcoder = GetTranscoder<TAsset>();
        try
        {
            var asset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            if (IsUpToDate(asset, FileSystem))
            {
                return asset;
            }

            (asset.Value as IDisposable)?.Dispose();
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            // The asset was never built, or was built by a different transcoder version
        }

        // Deliberately not guarded: if what we just built still fails to decode that is a bug in the
        // transcoder and the exception should reach the caller
        await Encode(id, settings);
        return await AssetDecoder.Decode(id, transcoder, FileSystem);
    }

    internal void HotSwap<TAsset>(TAsset instance, TAsset replacement)
    {
        var transcoder = GetTranscoder<TAsset>();
        transcoder.HotSwap(instance, replacement);
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
