using CapriKit.Concurrency.Async;
using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline.vNext;

public sealed partial class AssetManager : IDisposable
{
    // TODO: ensure that the transcoders and hot-reload manager are thread safe
    // TODO: what if an asset is requested multiple times? Do we keep track
    // of in-flight loading? Same for hot-reloading.

    private readonly ILogger<AssetManager> Logger;
    private readonly IVirtualFileSystem FileSystem;
    private readonly AssetCache Cache;
    private readonly HotReloadManager HotReloadManager;
    private readonly ConcurrentDictionary<Type, IAssetTranscoder> Transcoders;

    public AssetManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new AssetCache();
        HotReloadManager = new HotReloadManager(logger, fileSystem);
        Transcoders = [];
    }

    // Thread safe
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

    // Thread safe
    public AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var handle = new AssetHandle<TAsset>();

        // Check if the asset was loaded before
        if (Cache.TryLease<TAsset>(id, out var cachedAsset))
        {
            LogLoadedFromCache(Logger, id);
            handle.Resolve(cachedAsset);
            return handle;
        }

        Task.Run(() => BuildAsset(id, settings, handle)).FireAndForget(
                ex =>
                {
                    LogFailed(Logger, id);
                    handle.Resolve(ex);
                });

        return handle;
    }

    // Thread safe is the AssetDecoder methods are thread safe
    // TODO: take special care about the file that the asset is output to and when the copying of the file is resolved
    // though that might be more of a thing for the hot reloader to worry about it can happen if the file
    // is loaded twice.
    // TODO: HIGH! If the same asset is requested multiple times at startup its build or loaded multiple times.
    private async Task BuildAsset<TAsset, TSettings>(AssetId id, TSettings settings, AssetHandle<TAsset> handle)
        where TAsset : class
    {
        TAsset asset;
        var transcoder = GetTranscoder<TAsset, TSettings>();

        // Check if the asset can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build, FileSystem))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            asset = RegisterAsset(upToDateAsset, transcoder);
            handle.Resolve(asset);

            LogLoadedFromFile(Logger, id);
        }

        // If not, try to rebuild and load the asset
        if (!FileSystem.Exists(id.Path))
        {
            throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
        }

        await AssetEncoder.Encode(id, transcoder, settings, FileSystem);
        var freshAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
        asset = RegisterAsset(freshAsset, transcoder);
        handle.Resolve(asset);

        LogBuildAndLoaded(Logger, id);
    }

    // Thread safe
    public void Unload(AssetId id)
    {
        Cache.Return(id);
    }

    // Should only be called from the main thread, though technically thread safe
    public void Update()
    {
        Cache.Collect();
        HotReloadManager.Update();
    }

    public void Dispose()
    {
        Cache.Dispose();
        HotReloadManager.Dispose();
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
            if (!fileSystem.Exists(file))
            {
                return false;
            }

            var lastWrite = fileSystem.LastWriteTime(file);
            if (version != lastWrite)
            {
                return false;
            }
        }

        return true;
    }

    // TODO: ensure that HotReloadManager.Track is thread safe
    private TAsset RegisterAsset<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        HotReloadManager.Track(asset, transcoder);
        return Cache.PutOrLease(asset.Id, asset.Value);
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
