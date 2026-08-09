using CapriKit.IO;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline.v2;

internal sealed class HotReloader : IDisposable
{
    private abstract class Reloadable(AssetId id)
    {
        public AssetId Id { get; } = id;
        public abstract Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<Action> hotSwapActionQueue);
    }

    private sealed class Reloadable<TAsset, TSettings> : Reloadable
        where TAsset : class
    {
        private readonly WeakReference<Asset<TAsset, TSettings>> Instance;
        private readonly IAssetTranscoder<TAsset, TSettings> Transcoder;


        public Reloadable(AssetId id, Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
            : base(id)
        {
            Instance = new WeakReference<Asset<TAsset, TSettings>>(asset);
            Transcoder = transcoder;
        }

        public override async Task Reload(IVirtualFileSystem fileSystem, ConcurrentQueue<Action> hotSwapActionQueue)
        {
            if (!Instance.TryGetTarget(out var cold)) { return; }

            await AssetEncoder.Encode(cold.Id, Transcoder, cold.BuildMetaData.Settings, fileSystem);
            var hot = await AssetDecoder.Decode(cold.Id, Transcoder, fileSystem);
            hotSwapActionQueue.Enqueue(() => Transcoder.HotSwap(cold.Value, hot.Value));
        }
    }



    private static readonly TimeSpan MinWaitTime = TimeSpan.FromSeconds(0.5);
    private readonly ILogger<HotReloader> Logger;

    private readonly AssetManager AssetManager;
    private readonly IReadOnlyVirtualFileSystem FileSystem;

    private long lastFileChange;

    public void Track<TAsset, TSettings>(Asset<TAsset, TSettings> asset, IAssetTranscoder<TAsset, TSettings> transcoder)
        where TAsset : class
    {

    }
}
