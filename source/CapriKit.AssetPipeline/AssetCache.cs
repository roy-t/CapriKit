using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Simple cache that uses reference counting to decide when to clean-up a resource. Assets can be leased and returned at any time (though the class requires single-threaded access).
/// The actual disposing of objects only happens when the main thread calls <see cref="Collect"/>.
/// </summary>
internal sealed class AssetCache : IDisposable
{
    private class Line(object Asset, int RefCount)
    {
        public readonly object asset = Asset;
        public int refCount = RefCount;
    }

    private readonly Lock Lock = new();
    private readonly Dictionary<AssetId, Line> Lines = [];

    public void Put<TAsset>(AssetId id, TAsset asset)
        where TAsset : class
    {
        lock (Lock)
        {
            if (Lines.ContainsKey(id))
            {
                throw new Exception($"Cache already contains asset: {id}.");
            }

            var entry = new Line(asset, 1);
            Lines.Add(id, entry);
        }
    }

    public bool TryLease<TAsset>(AssetId id, [NotNullWhen(true)] out TAsset? asset)
        where TAsset : class
    {
        lock (Lock)
        {
            if (Lines.TryGetValue(id, out var entry))
            {
                entry.refCount = entry.refCount + 1;
                asset = (TAsset)entry.asset;
                return true;
            }
        }

        asset = default;
        return false;
    }

    public void Return(AssetId id)
    {
        lock (Lock)
        {
            var entry = Lines[id];
            entry.refCount = entry.refCount - 1;
        }
    }

    // TODO: can we dispose objects in the background or would that make the GPU unhappy when assets like textures are disposed at random times?
    public void Collect()
    {
        List<AssetId>? toCollect = null;
        foreach (var (key, value) in Lines)
        {
            if (value.refCount <= 0)
            {
                toCollect = toCollect ?? [];
                toCollect.Add(key);
            }
        }

        if (toCollect == null) { return; }

        foreach (var key in toCollect)
        {
            Lines.Remove(key, out var entry);
            (entry as IDisposable)?.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var value in Lines.Values)
        {
            (value as IDisposable)?.Dispose();
        }
        Lines.Clear();
    }
}
