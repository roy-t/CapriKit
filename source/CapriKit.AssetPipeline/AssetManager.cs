using CapriKit.AssetPipeline.HotReloading;
using CapriKit.IO;
using Microsoft.Extensions.Logging;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Encodes, decodes, loads and tracks assets.
/// </summary>
public sealed class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;
    private readonly TranscoderCollection Transcoders;

    private readonly AssetMemoryCache Cache;
    private readonly AssetFileCache FileCache;
    private readonly HotSwapManager HotSwapManager;

    public AssetManager(ILoggerFactory logger, DirectoryPath rootDirectory)
        : this(logger, new FileSystem().ScopedTo(rootDirectory)) { }

    public AssetManager(ILoggerFactory logger, IVirtualFileSystem fileSystem)
    {
        FileSystem = fileSystem;
        Transcoders = new TranscoderCollection();
        Cache = new AssetMemoryCache();
        FileCache = new AssetFileCache(fileSystem, Transcoders);
        HotSwapManager = new HotSwapManager(logger, this, fileSystem);
    }

    /// <inheritdoc cref="AssetMemoryCache.PushScope"/>
    public void PushScope() => Cache.PushScope();

    /// <inheritdoc cref="AssetMemoryCache.PopScope"/>
    public void PopScope() => Cache.PopScope();

    /// <inheritdoc cref="TranscoderCollection.Register"/>
    public void RegisterTranscoder<TAsset>(IAssetTranscoder<TAsset> transcoder)
    {
        Transcoders.Register(transcoder);
    }

    public Task Encode<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
    {
        return AssetEncoder.Encode(id, settings, Transcoders.Get<TAsset>(), FileSystem);
    }

    public Task Encode<TAsset>(AssetId id)
        where TAsset : class
    {
        return Encode(id, default(NoSettings<TAsset>));
    }

    /// <summary>
    /// Immediately decodes an asset, bypasses cache and hot reloading mechanisms.
    /// </summary>
    public Task<AssetJob<TAsset>> Decode<TAsset>(AssetId id)
        where TAsset : class
    {
        return AssetDecoder.Decode(id, Transcoders.Get<TAsset>(), FileSystem);
    }

    /// <summary>
    /// Loads an asset from the cache, decoding it from disk and building it first if it is missing or
    /// out of date. The first time an asset is loaded it is put in the current scope, see <see cref="PushScope"/>.
    /// </summary>
    public async Task<TAsset> Load<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        // Live asset: ready for use.
        if (Cache.TryGet<TAsset>(id, out var entry))
        {
            return entry;
        }

        // Matching encoded version available on disk: decode, register for hot swapping and return.
        var getFromFileCache = await FileCache.Load(id, settings);
        if (getFromFileCache.OnSuccess(out var cachedAsset))
        {
            Cache.Add(id, cachedAsset.Value);
            HotSwapManager.Track(cachedAsset, settings);
            return cachedAsset.Value;
        }

        if (getFromFileCache.OnFailure(out var exception))
        {
            // TODO: log
        }

        // Encoded version unavailable, out of date or created using different settings:
        // Encode, decode, register for hot swapping and return.
        await Encode(id, settings);
        var getFromFullBuild = await Decode<TAsset>(id);
        if (getFromFullBuild.OnSuccess(out var freshAsset))
        {
            Cache.Add(id, freshAsset.Value);
            HotSwapManager.Track(freshAsset, settings);
        }

        if (getFromFileCache.OnFailure(out var rebuildFailure))
        {
            rebuildFailure.Throw();
        }

        throw new Exception($"Asset {id} could not be found");
    }

    internal void HotSwap<TAsset>(TAsset instance, TAsset replacement)
    {
        var transcoder = Transcoders.Get<TAsset>();
        transcoder.HotSwap(instance, replacement);
    }
}
