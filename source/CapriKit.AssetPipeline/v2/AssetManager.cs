using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace CapriKit.AssetPipeline.v2;

// TODO: Add logging
public sealed class AssetManager : IDisposable
{
    private readonly IVirtualFileSystem FileSystem;
    private readonly AssetCache Cache;
    private readonly ILogger<AssetManager> Logger;

    public AssetManager(ILoggerFactory logger, IVirtualFileSystem fileSystem)
    {
        Logger = logger.CreateLogger<AssetManager>();
        FileSystem = fileSystem;
        Cache = new AssetCache();
    }

    public async Task<TAsset> Load<TAsset, TSettings>(AssetId id, IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings)
        where TAsset : class
    {
        // Check if the asset was loaded before
        if (Cache.TryLease<TAsset>(id, out var cachedAsset))
        {
            return cachedAsset;
        }

        // If not, check if it can be loaded from an up-to-date build
        var build = await AssetDecoder.TryDecodeBuildMetaData(id, transcoder, FileSystem);
        if (build != null && IsUpToDate(transcoder, settings, build))
        {
            var upToDateAsset = await AssetDecoder.Decode(id, transcoder, FileSystem);
            RegisterAsset(upToDateAsset, transcoder);
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

        return freshAsset.Value;
    }

    public void Unload(AssetId id)
    {
        Cache.Return(id);
    }

    /// <remarks>Must be called from the main thread.</remarks>
    public void Update()
    {
        // TODO: Perform work that can only be done on the main thread (like hot-reloading)
        Cache.Collect();
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
            if (version < lastWrite)
            {
                return false;
            }
        }

        return true;
    }

    private void RegisterAsset<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {
        // TODO: Register for hot reloading
        Cache.Put(asset.Id, asset.Value);
    }

    public void Dispose()
    {
        Cache.Dispose();
    }
}
