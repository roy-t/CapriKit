using CapriKit.Concurrency.Async;
using CapriKit.Concurrency.Primitives;
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
    private readonly ScopedFileSystem FileSystem;
    private readonly AssetCache Cache;
    private readonly HotReloadManager HotReloadManager;
    private readonly ConcurrentDictionary<Type, IAssetTranscoder> Transcoders;

    private readonly LightweightChannel<(AssetId, Func<object>)> Incoming;
    private readonly Lock RequestLock;
    private readonly Dictionary<AssetId, List<AssetHandle>> Outstanding;

    public AssetManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new();
        HotReloadManager = new(logger, fileSystem);
        Transcoders = [];
        Incoming = new();
        RequestLock = new();
        Outstanding = [];
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

        // Ensure we can't miss an asset being done loading or being requested by another.
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

    // Thread safe is the AssetDecoder methods are thread safe
    // TODO: take special care about the file that the asset is output to and when the copying of the file is resolved
    // though that might be more of a thing for the hot reloader to worry about it can happen if the file
    // is loaded twice.
    private async Task RequestAsset<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var transcoder = GetTranscoder<TAsset, TSettings>();

        // Check if the asset can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build, FileSystem))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            Incoming.Write((id, () => RegisterAsset(upToDateAsset, transcoder)));
            LogLoadedFromFile(Logger, id);
        }

        // If not, try to rebuild and load the asset
        if (!FileSystem.Exists(id.Path))
        {
            throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
        }

        await AssetEncoder.Encode(id, transcoder, settings, FileSystem);
        var freshAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
        Incoming.Write((id, () => RegisterAsset(freshAsset, transcoder)));
        LogBuildAndLoaded(Logger, id);
    }

    // Thread safe
    public void Unload(AssetId id)
    {
        Cache.Return(id);
    }

    // Should only be called from the primary thread, though technically thread safe
    public void Update()
    {
        lock (RequestLock)
        {
            while (Incoming.TryRead(out var result))
            {
                var (id, retriever) = result;
                var handles = Outstanding[id];
                foreach (var handle in handles)
                {
                    var asset = retriever();
                    handle.Resolve(asset);
                }
                Outstanding.Remove(id);
            }
        }

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
        var actualObject = Cache.PutOrLease(asset.Id, asset.Value);
        var actualWrapper = new Asset<TAsset, TSettings>(asset.Id, actualObject, asset.BuildMetaData);
        HotReloadManager.Track(actualWrapper, transcoder); // TODO: track needs to be thread safe and ignore adding the same thing multiple times!
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
