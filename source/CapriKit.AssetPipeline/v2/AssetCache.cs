using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.v2;

internal sealed class AssetCache
{
    private class CacheEntry(object Asset, int RefCount)
    {
        public readonly object asset = Asset;
        public int refCount = RefCount;
    }

    private readonly Lock Lock = new();

    private readonly Dictionary<AssetId, CacheEntry> Entries = [];

    public void Put<TAsset>(AssetId id, TAsset asset)
        where TAsset : class
    {
        lock (Lock)
        {
            if (Entries.ContainsKey(id))
            {
                throw new Exception($"Cache already contains asset: {id}.");
            }

            var entry = new CacheEntry(asset, 1);
            Entries.Add(id, entry);
        }
    }

    public bool TryLease<TAsset>(AssetId id, [NotNullWhen(true)] out TAsset? asset)
        where TAsset : class
    {
        lock (Lock)
        {
            if (Entries.TryGetValue(id, out var entry))
            {
                entry.refCount = entry.refCount + 1;
                asset = (TAsset)entry.asset;
                return true;
            }
        }

        asset = default;
        return false;
    }

    public bool TryRelease<TAsset>(AssetId id, [NotNullWhen(true)] out IDisposable? disposable)
        where TAsset : class
    {
        lock (Lock)
        {
            if (Entries.TryGetValue(id, out var entry))
            {
                entry.refCount = entry.refCount - 1;
                if (entry.refCount == 0 && entry.asset is IDisposable disposableAsset)
                {
                    disposable = disposableAsset;
                    return true;
                }
            }
        }

        disposable = default;
        return false;
    }
}
