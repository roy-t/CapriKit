using System.Collections.Concurrent;

namespace CapriKit.AssetPipeline.HotReloading;

internal sealed class HotSwapQueue
{
    private interface IHotSwappable
    {
        public void HotSwap(AssetManager manager);
    }

    private sealed class HotSwappable<TAsset>(TAsset cold, TAsset hot) : IHotSwappable
    {
        public void HotSwap(AssetManager manager)
        {
            manager.HotSwap(cold, hot);
        }
    }

    private readonly ConcurrentQueue<IHotSwappable> Queue = [];

    public void Enqueue<TAsset>(TAsset cold, TAsset hot)
    {
        Queue.Enqueue(new HotSwappable<TAsset>(cold, hot));
    }

    public void HotSwapPending(AssetManager manager)
    {
        while (Queue.TryDequeue(out var result))
        {
            result.HotSwap(manager);
        }
    }
}
