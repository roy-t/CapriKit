using CapriKit.IO;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline.v2;

internal sealed record HotSwapAction(AssetId Id, Action PerformHotSwap);

internal abstract class HotReloadable(AssetId id)
{
    public AssetId Id { get; } = id;
    public abstract Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<HotSwapAction> hotSwapActionQueue);
}

internal sealed class HotReloadable<TAsset, TSettings> : HotReloadable
    where TAsset : class
{
    private readonly WeakReference<Asset<TAsset, TSettings>> Instance;
    private readonly IAssetTranscoder<TAsset, TSettings> Transcoder;


    public HotReloadable(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        : base(asset.Id)
    {
        Instance = new WeakReference<Asset<TAsset, TSettings>>(asset);
        Transcoder = transcoder;
    }

    public override async Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<HotSwapAction> hotSwapActionQueue)
    {
        if (!Instance.TryGetTarget(out var cold)) { return; }

        await AssetEncoder.Encode(cold.Id, Transcoder, cold.BuildMetaData.Settings, fileSystem);
        var hot = await AssetDecoder.Decode(cold.Id, Transcoder, fileSystem);

        hotSwapActionQueue.Enqueue(new HotSwapAction(cold.Id, () => Transcoder.HotSwap(cold.Value, hot.Value)));
    }
}
