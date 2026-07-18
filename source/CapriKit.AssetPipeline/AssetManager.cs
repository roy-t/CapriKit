using CapriKit.IO;

namespace CapriKit.AssetPipeline;

public class AssetManager
{
    private readonly IVirtualFileSystem FileSystem;
    private readonly Dictionary<Type, IAssetTranscoder> Transcoders = [];

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
        var typeKey = typeof(TAsset);
        Transcoders[typeKey] = transcoder;
    }

    public void Encode<TAsset, TSettings>(AssetId id, TSettings settings)
        where TSettings : IAssetSettings<TAsset>
    {
        var typeKey = typeof(TAsset);
        var transcoder = Transcoders[typeKey];

        throw new NotImplementedException();
    }

    public TAsset Decode<TAsset, TSettings>(AssetId id, TSettings settings)
        where TSettings : IAssetSettings<TAsset>
    {
        var typeKey = typeof(TAsset);
        var transcoder = Transcoders[typeKey];

        throw new NotImplementedException();
    }

    // TODO: the current typing and generic constraints are neat for writing transcoders but encoding/decoding requires all
    // type parameters as type inference doesn't pick up that TAsset can be derived from that kind of IAssetSettings<X> TSettings is.
    /*
        AssetManager m;
        var settings = new NoSettings<string>();
        m.RegisterTranscoder(transcoder);
        m.Encode<string, NoSettings<string>>(id, settings);
        m.Decode<string, NoSettings<string>>(id, settings);
    */
}
