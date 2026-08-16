//using CapriKit.Concurrency.Primitives;

//namespace CapriKit.AssetPipeline;

//internal abstract record HotSwapRecipe
//{
//    public abstract void HotSwap();
//}


//internal abstract record ReloadRecipe
//{
//    public abstract Task<HotSwapRecipe> Reload();
//}

//internal abstract record ReloadRecipe<TAsset, TSettings>(AssetId Id, AssetBuildMetaData<TSettings> Metadata, IAssetTranscoder<TAsset, TSettings> Transcoder)
//    : ReloadRecipe
//    where TAsset : class
//{
//    public override Task<HotSwapRecipe> Reload()
//    {

//    }
//}


//internal sealed class HotReloadPipeline
//{
//    private readonly LightweightChannel<ReloadRecipe> WaitingForRebuild = new();
//    private readonly LightweightChannel<HotSwapRecipe> WaitingForHotSwap = new();

//    private volatile bool isEnabled = true;
//    private volatile bool isWorking = false;
//    private Task<HotSwapRecipe>? reloadTask = null;
//    private Lock StateLock = new Lock();

//    public bool TryEnter(ReloadRecipe recipe)
//    {
//        lock (StateLock)
//        {
//            if (!isEnabled) { return false; }

//            WaitingForRebuild.Write(recipe);
//            return true;
//        }
//    }

//    public void Update()
//    {
//        lock (StateLock)
//        {
//            if (isWorking)
//            {
//                if (reloadTask != null && reloadTask.IsCompletedSuccessfully)
//                {
//                    WaitingForHotSwap.Write(reloadTask.Result);
//                }
//            }
//            else
//            {

//                if (WaitingForRebuild.TryRead(out var rebuild))
//                {
//                    reloadTask = rebuild.Reload();
//                    reloadTask.Start(); // TODO: is this necessary?
//                    isWorking = true;
//                }
//            }
//        }


//        while (WaitingForHotSwap.TryRead(out var hotswap))
//        {
//            hotswap.HotSwap();
//        }
//    }
//}
