using CapriKit.Concurrency.Primitives;

namespace CapriKit.AssetPipeline.vNext;

public sealed partial class AssetManager : IDisposable
{
    // TODO: register and then get transcoders
    private readonly Dictionary<AssetId, (AssetBundle Bundle, Promise Promise)> OutstandingPromises = [];
    private readonly LightweightChannel<(AssetId Id, object Asset)> Ready = new();
    private readonly AssetCache Cache = new();

    public bool TryLoad<TAsset, TSetting>(AssetId id, TSetting settings)
        where TAsset : class
    {

        // TODO: Check if the file is in the AssetCache
        // TODO: Otherwise kick off the loading process
        // TODO: build to random output file and only move it to the right one once the main thread drains it.
        throw new NotImplementedException();
    }

    public Promise<TAsset> Load<TAsset, TSetting>(AssetId id, TSetting settings)
        where TAsset : class
    {
        var promise = new Promise<TAsset>();


        throw new NotImplementedException();
    }

    public void Update()
    {
        // TODO: what if 2 promises wait for the same thing, multi-threading
        // TODO: what if an asset is already done loading (or was already loaded) before the bundle was created

        while (Ready.TryRead(out var kv))
        {
            if (OutstandingPromises.Remove(kv.Id, out var kv2)) 
            {
                var asset = Cache.PutOrLease(kv.Id, kv.Asset);
                kv2.Promise.Value = asset;
                kv2.Bundle.OnRequestCompleted();
            }
        }
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
        return new AssetBundle<TBundle>(constructor);
    }
}
