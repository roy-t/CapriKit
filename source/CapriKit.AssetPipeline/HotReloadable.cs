using CapriKit.IO;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline;

internal sealed record HotSwapAction(AssetId Id, Action PerformHotSwap);

internal abstract class HotReloadable(AssetId id)
{
    public AssetId Id { get; } = id;
    public abstract Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<HotSwapAction> hotSwapActionQueue);
}

internal sealed class HotReloadable<TAsset, TSettings> : HotReloadable
    where TAsset : class
{
    private readonly WeakReference<TAsset> Instance;
    private readonly TSettings Settings;
    private readonly IAssetTranscoder<TAsset, TSettings> Transcoder;

    public HotReloadable(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        : base(asset.Id)
    {
        Instance = new WeakReference<TAsset>(asset.Value);
        Settings = asset.BuildMetaData.Settings;
        Transcoder = transcoder;
    }

    public override async Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<HotSwapAction> hotSwapActionQueue)
    {
        if (!Instance.TryGetTarget(out var cold)) { return; }

        await AssetEncoder.Encode(Id, Transcoder, Settings, fileSystem);
        var hot = await AssetDecoder.Decode(Id, Transcoder, fileSystem);

        hotSwapActionQueue.Enqueue(new HotSwapAction(Id, () => Transcoder.HotSwap(cold, hot.Value)));
    }
}
