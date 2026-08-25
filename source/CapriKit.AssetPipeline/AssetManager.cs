using CapriKit.Concurrency.Async;
using CapriKit.Concurrency.Primitives;
using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Manages the building, loading, chaching, clean-up and hot-reloading of assets.
/// </summary>
public sealed partial class AssetManager : IDisposable
{
    private readonly ILogger<AssetManager> Logger;
    private readonly ScopedFileSystem FileSystem;
    private readonly AssetPool Cache;
    private readonly HotReloadManager HotReloadManager;
    private readonly ConcurrentDictionary<Type, IAssetTranscoder> Transcoders;

    private readonly LightweightChannel<(AssetId Id, Func<object> Materializer)> Incoming;
    private readonly Lock RequestLock;
    private readonly Dictionary<AssetId, List<AssetHandle>> Outstanding;

    public AssetManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new();
        HotReloadManager = new(logger, Cache, FileSystem);
        Transcoders = [];
        Incoming = new();
        RequestLock = new();
        Outstanding = [];
    }

    /// <summary>
    /// Register a transcoder for the given asset type. Registering a transcoders for a type
    /// that was already assigned a transcoder throws an exception.
    /// Threading: thread-safe, multiple threads can register transcoders at the same time.
    /// </summary>
    public void RegisterTranscoder<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        var key = typeof(TAsset);
        var original = Transcoders.GetOrAdd(key, transcoder);
        if (original != transcoder)
        {
            throw new Exception($"A transcoder for key: {key.FullName} was already registered");
        }
    }

    /// <summary>
    /// Use to defines a bundle of assets to load.
    /// Threading: thread-safe.
    /// </summary>
    public AssetBundleBuilder CreateBundle()
    {
        return new AssetBundleBuilder(this);
    }

    /// <summary>
    /// Starts loading an asset. The asset will either be loaded from the cache, from disk, or rebuild and then loaded.
    /// The caller gets a handle to be used in an <see cref="AssetBundleLoader"/> which can be resolved
    /// to the actual asset when loading finishes using <see cref="AssetBundleLoader{T}.IsReady"/>
    /// Threading: thread-safe, can be called from any thread concurrently. This method guarantees that the same asset
    /// is not loaded multiple times concurrently.
    /// </summary>
    internal AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var handle = new AssetHandle<TAsset>(id);

        // At this time an asset is either already loaded, already requested or requested for the first time.
        // The lock ensure that this does not change while we check what we should do with the request.
        lock (RequestLock)
        {
            // Check if the asset was loaded before
            if (Cache.TryLease<TAsset>(id, out var cachedAsset))
            {
                LogLoadedFromCache(Logger, id);
                handle.Resolve(cachedAsset);
                return handle;
            }

            // Check if the asset was already requested
            if (Outstanding.TryGetValue(id, out var requestors))
            {
                requestors.Add(handle);
            }
            else
            {
                // Request the asset
                Outstanding[id] = [handle];
                Task.Run(() => RequestAsset<TAsset, TSettings>(id, settings)).FireAndForget(
                ex =>
                {
                    LogFailed(Logger, id);
                    Incoming.Write(ex);
                });
            }
        }

        return handle;
    }

    /// <summary>
    /// Performs the actual loading or building and loading of the asset.
    /// Threading: The caller has to guarantee that this method does not run concurrently for the same asset-id.
    /// </summary>
    private async Task RequestAsset<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var transcoder = GetTranscoder<TAsset, TSettings>();

        // Check if the asset can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build, FileSystem))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write((id, () => TrackAndTakeLease(upToDateAsset, transcoder)));
            LogLoadedFromFile(Logger, id);
        }
        else // If not, try to rebuild and load the asset
        {
            if (!FileSystem.Exists(id.Path))
            {
                throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
            }

            await AssetEncoder.Encode(id, transcoder, settings, FileSystem);
            var freshAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write((id, () => TrackAndTakeLease(freshAsset, transcoder)));
            LogBuildAndLoaded(Logger, id);
        }
    }

    /// <summary>
    /// Unloads all assets in the bundle.
    /// Threading: Unload updates the internal state of the bundle using a lock so that it is safe
    /// to unload the same bundle from multiple threads.
    /// </summary>
    public void Unload(AssetBundleLoader bundle) // TODO: this should unload an assetbundle, not a loader, but that is not a formal class/interface yet and I don't want it to become more complicated to define an asset bundle. What to do?
    {
        try
        {
            RequestLock.Enter();
            if (bundle.IsActive)
            {
                foreach (var asset in bundle.Assets)
                {
                    Cache.Return(asset);
                }
            }
        }
        finally
        {
            bundle.IsActive = false;
            RequestLock.Exit();
        }
    }

    /// <summary>
    /// Materializes assets that have finished loading, removes unused items from the cache
    /// and hot-reloads changed assets.
    /// Threading: Should only be called from the primary thread.
    /// </summary>
    public void Update()
    {
        // Ensure that while we check which assets are done loading and up-to-date that administration
        // a new request to `Load` of the same asset does not miss these update.
        lock (RequestLock)
        {
            while (Incoming.TryRead(out var result))
            {
                var (id, trackAndTakeLease) = result;
                var handles = Outstanding[id];
                foreach (var handle in handles)
                {
                    var asset = trackAndTakeLease();
                    handle.Resolve(asset);
                }
                Outstanding.Remove(id);
            }
        }

        Cache.DisposeReleased();
        HotReloadManager.Update();
    }

    public void Dispose()
    {
        // Dispose the hot-reload manager first so that it can release any reference to
        // assets it might still hold.
        HotReloadManager.Dispose();
        Cache.Dispose();
    }

    // Thread safe, only touches the file system and uses thread safe transcoder methods and properties.
    private static bool IsUpToDate<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings, AssetBuildMetaData<TSettings> build, IReadOnlyVirtualFileSystem fileSystem)
        where TAsset : class
    {
        // Transcoders differ
        if (transcoder.Id != build.TranscoderId || transcoder.Version != build.TranscoderVersion)
        {
            return false;
        }

        // Settings differ
        var inUse = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(settings, inUse);

        var inFile = new ArrayBufferWriter<byte>();
        transcoder.WriteSettings(build.Settings, inFile);

        if (!inUse.WrittenSpan.SequenceEqual(inFile.WrittenSpan))
        {
            return false;
        }

        // Dependencies have changed
        foreach (var (file, version) in build.Dependencies)
        {
            // Treat a file as up-to-date if does not exist
            if (fileSystem.Exists(file))
            {
                // Otherwise double check the file date matches
                var lastWrite = fileSystem.LastWriteTime(file);
                if (version != lastWrite)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Used when assigning newly loaded assets to their handles. Puts the new asset into the cache,
    /// tracks it and then takes a lease for the handle.
    /// </summary>
    private TAsset TrackAndTakeLease<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // Though the asset manager does not allow loading the same asset multiple times the cache contract does allow it
        var actualObject = Cache.PutOrLease(asset.Id, asset.Value);

        var actualWrapper = new Asset<TAsset, TSettings>(asset.Id, actualObject, asset.BuildMetaData);
        HotReloadManager.Track(actualWrapper, transcoder);
        return actualObject;
    }

    // Thread safe because Transcoders is thread safe
    private IAssetTranscoder<TAsset, TSetting> GetTranscoder<TAsset, TSetting>() where TAsset : class
    {
        var type = typeof(TAsset);
        if (!Transcoders.TryGetValue(type, out var transcoder))
        {
            throw new Exception($"Missing transcoder for asset type: {type.FullName}");
        }

        if (transcoder is not IAssetTranscoder<TAsset, TSetting> specializedTranscoder)
        {
            throw new Exception($"Expected transcoder of type: {type.FullName} but got transcoder of type: {transcoder.GetType().FullName}");
        }

        return specializedTranscoder;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded asset: {asset} from cache")]
    private static partial void LogLoadedFromCache(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded up-to-date asset: {asset} from file")]
    private static partial void LogLoadedFromFile(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Build and loaded fresh asset: {asset}")]
    private static partial void LogBuildAndLoaded(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Error, Message = "Building or loading asset: {asset} failed.")]
    private static partial void LogFailed(ILogger logger, AssetId asset);
}
