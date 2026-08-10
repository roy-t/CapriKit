namespace CapriKit.AssetPipeline.vNext;

internal sealed partial class AssetManager : IDisposable
{


    public bool TryLoad<TAsset, TSetting>(AssetId id, IAssetTranscoder<TAsset, TSetting> transcoder, TSetting settings)
        where TAsset : class
    {
        // TODO: Check if the file is in the AssetCache
        // TODO: Otherwise kick off the loading process
        // TODO: build to random output file and only move it to the right one once the main thread drains it.
        throw new NotImplementedException();
    }


    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
