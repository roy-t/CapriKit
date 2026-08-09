using System;
using System.Collections.Generic;
using System.Text;

namespace CapriKit.AssetPipeline.vNext;

internal sealed partial class AssetManager : IDisposable
{


    public bool TryLoad<TAsset, TSetting>(AssetId id, IAssetTranscoder<TAsset, TSetting> transcoder, TSetting settings)
        where TAsset : class
    {
        if()
    }


    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
