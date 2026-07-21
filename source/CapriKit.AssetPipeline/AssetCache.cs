using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Caches assets in stacked scopes that can be discarded in one go.
/// Assets are released when the scope they were added in is popped or this class is disposed
/// </summary>
public sealed class AssetCache : IDisposable
{
    private record CacheItem(int Scope, object Item, IDisposable? Disposable);

    private readonly Dictionary<AssetId, CacheItem> Cache;
    private int scope;

    public AssetCache()
    {
        scope = 0;
        Cache = [];
    }

    /// <summary>
    /// Opens a new scope. Assets added from now on are discarded by the matching <see cref="PopScope"/>.
    /// </summary>
    public void PushScope()
    {
        scope++;
    }

    /// <summary>
    /// Adds an asset to the current scope.
    /// </summary>
    /// <exception cref="InvalidOperationException">Adding an item with the same id twice throws</exception>
    public void Add<T>(AssetId id, T asset)
        where T : class
    {
        if (Cache.ContainsKey(id))
        {
            throw new InvalidOperationException($"Cannot add item with id: {id} a second time");
        }
        Cache[id] = new CacheItem(scope, asset, asset as IDisposable);
    }

    public bool TryGet<T>(AssetId id, [NotNullWhen(true)] out T? asset)
        where T : class
    {
        if (Cache.TryGetValue(id, out var entry))
        {
            asset = (T)entry.Item;
            return true;
        }

        asset = default;
        return false;
    }

    /// <summary>
    /// Pops the current scope, discarding and disposing every asset added within it.
    /// </summary>
    public void PopScope()
    {
        if (scope <= 0)
        {
            throw new InvalidOperationException("No scope to pop");
        }

        foreach (var (key, value) in Cache)
        {
            if (value.Scope >= scope)
            {
                value.Disposable?.Dispose();
                Cache.Remove(key);
            }
        }

        scope--;
    }

    public void Dispose()
    {
        foreach (var (_, value) in Cache)
        {
            value.Disposable?.Dispose();
        }

        Cache.Clear();
    }
}
