using CapriKit.Concurrency.Promises;

namespace CapriKit.AssetPipeline.vNext;

public sealed partial class AssetManager : IDisposable
{


    public bool TryLoad<TAsset, TSetting>(AssetId id, IAssetTranscoder<TAsset, TSetting> transcoder, TSetting settings)
        where TAsset : class
    {
        // TODO: Check if the file is in the AssetCache
        // TODO: Otherwise kick off the loading process
        // TODO: build to random output file and only move it to the right one once the main thread drains it.
        throw new NotImplementedException();
    }


    public Promise<TAsset> Load<TAsset, TSetting>(AssetId id, IAssetTranscoder<TAsset, TSetting> transcoder, TSetting settings)
        where TAsset : class
    {
        throw new NotImplementedException();
    }


    public void Dispose()
    {
        throw new NotImplementedException();
    }

    internal AssetBundle<TBundle> Bundle<TBundle>(Func<PromiseResolver, TBundle> constructor)
    {
        throw new NotImplementedException();

        // TODO: do something so that bundle gets notified when something is done loading
        // or maybe the actual loading should only start here?
        return new AssetBundle<TBundle>(1, constructor);
    }
}
