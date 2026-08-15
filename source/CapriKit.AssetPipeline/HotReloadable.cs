using CapriKit.IO;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline;

internal sealed record HotSwapAction(AssetId Id, IReadOnlyList<Dependency> Dependencies, Action PerformHotSwap);

internal abstract class HotReloadable(AssetId id)
{
    public AssetId Id { get; } = id;
    public abstract bool IsAlive { get; }

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

    public override bool IsAlive => Instance.TryGetTarget(out var _);

    public override async Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<HotSwapAction> hotSwapActionQueue)
    {
        if (!Instance.TryGetTarget(out var cold)) { return; }

        // We store the encoded asset in memory instead of on disk to prevent
        // touching the file while other threads are also working on it.
        using var stream = new MemoryStream();
        await AssetEncoder.Encode(Id, Transcoder, Settings, fileSystem, stream);

        stream.Seek(0, SeekOrigin.Begin);
        var hot = await AssetDecoder.Decode(Id, Transcoder, fileSystem, stream);

        hotSwapActionQueue.Enqueue(new HotSwapAction(Id, hot.BuildMetaData.Dependencies, () => Transcoder.HotSwap(cold, hot.Value)));        
    }
}
