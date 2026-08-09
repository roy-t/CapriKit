using CapriKit.AssetPipeline.v2;
using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace CapriKit.AssetPipeline;

public sealed partial class AssetManager : IDisposable
{
    private readonly ILogger<AssetManager> Logger;
    private readonly IVirtualFileSystem FileSystem;
    private readonly AssetCache Cache;
    private readonly HotReloadManager HotReloadManager;

    public AssetManager(ILoggerFactory logger, ScopedFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new AssetCache();
        HotReloadManager = new HotReloadManager(logger, fileSystem);
    }

    public async Task<TAsset> Load<TAsset, TSettings>(AssetId id, IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings)
        where TAsset : class
    {
        // Check if the asset was loaded before
        if (Cache.TryLease<TAsset>(id, out var cachedAsset))
        {
            LogLoadedFromCache(Logger, id);
            return cachedAsset;
        }

        // If not, check if it can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != default && IsUpToDate(transcoder, settings, build))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            RegisterAsset(upToDateAsset, transcoder);

            LogLoadedFromFile(Logger, id);
            return upToDateAsset.Value;
        }

        // If not, try to rebuild and load the asset
        if (!FileSystem.Exists(id.Path))
        {
            throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
        }

        await AssetEncoder.Encode(id, transcoder, settings, FileSystem);
        var freshAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
        RegisterAsset(freshAsset, transcoder);

        LogBuildAndLoaded(Logger, id);
        return freshAsset.Value;
    }

    public void Unload(AssetId id)
    {
        Cache.Return(id);
    }

    /// <remarks>Must be called from the main thread.</remarks>
    public void Update()
    {
        Cache.Collect();
        HotReloadManager.Update();
    }

    private bool IsUpToDate<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings, AssetBuildMetaData<TSettings> build)
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
            if (!FileSystem.Exists(file))
            {
                return false;
            }

            var lastWrite = FileSystem.LastWriteTime(file);
            if (version != lastWrite)
            {
                return false;
            }
        }

        return true;
    }

    private void RegisterAsset<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        Cache.Put(asset.Id, asset.Value);
        HotReloadManager.Track(asset, transcoder);
    }

    public void Dispose()
    {
        Cache.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded asset: {asset} from cache")]
    private static partial void LogLoadedFromCache(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded up-to-date asset: {asset} from file")]
    private static partial void LogLoadedFromFile(ILogger logger, AssetId asset);

    [LoggerMessage(Level = LogLevel.Information, Message = "Build and loaded fresh asset: {asset}")]
    private static partial void LogBuildAndLoaded(ILogger logger, AssetId asset);
}
