using CapriKit.IO;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.v2;

public record class BuildMetaData(AssetId Id, IReadOnlyList<Dependency> Dependencies);

public sealed class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;
    private readonly TranscoderCollection Transcoders;

    /// <inheritdoc cref="TranscoderCollection.Register"/>
    public void RegisterTranscoder<TAsset>(IAssetTranscoder<TAsset> transcoder)
    {
        Transcoders.Register(transcoder);
    }

    public async Task<TAsset> Load<TAsset>(AssetId id, IAssetSettings<TAsset> settings)
        where TAsset : class
    {
        if (LoadFromCache<TAsset>(id, out var cachedAsset))
        {
            return cachedAsset;
        }

        var transcoder = Transcoders.Get<TAsset>();
        if (LoadBuildMetadata(id, settings, transcoder, out var build) && IsUpToDate(build))
        {
            return await Decode(id, settings, transcoder);
        }

        // The asset was not build or is out of date

        if (!SourceFileExists(id))
        {
            throw new FileNotFoundException("Could not find primary file to build asset from", id.Path);
        }

        await Encode(id, settings, transcoder);
        var asset = await Decode(id, settings, transcoder);
        RegisterAsset(id, asset, settings);

        return asset;
    }

    public void Unload(AssetId id)
    {
        // TODO: Decrease the reference count in the cache and if refcount == 0 remove from hot reloading and dispose
        throw new NotImplementedException();
    }

    /// <remarks>Must be called from the main thread.</remarks>
    public void Update()
    {
        // TODO: Perform work that can only be done on the main thread (like hot-reloading)
        throw new NotImplementedException();
    }

    private async Task Encode<TAsset>(AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> transcoder) where TAsset : class
    {
        throw new NotImplementedException();
    }

    private async Task<TAsset> Decode<TAsset>(AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> transcoder) where TAsset : class
    {
        // LOad from file
        // Add to cache
        throw new NotImplementedException();
    }

    private bool LoadFromCache<TAsset>(AssetId id, [NotNullWhen(true)] out TAsset? asset)
        where TAsset : class
    {
        throw new NotImplementedException();
    }

    private bool LoadBuildMetadata<TAsset>(AssetId id, IAssetSettings<TAsset> settings, IAssetTranscoder<TAsset> transcoder, [NotNullWhen(true)] out BuildMetaData? build)
        where TAsset : class
    {
        throw new NotImplementedException();
    }

    private bool IsUpToDate(BuildMetaData build)
    {
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
    private bool SourceFileExists(AssetId id)
    {
        throw new NotImplementedException();
    }

    private void RegisterAsset<TAsset>(AssetId id, TAsset asset, IAssetSettings<TAsset> settings) where TAsset : class
    {
        // Add to cache
        // Register for hot reloading
        throw new NotImplementedException();
    }
}
