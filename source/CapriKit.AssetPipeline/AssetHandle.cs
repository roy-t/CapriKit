using System.Diagnostics;

namespace CapriKit.AssetPipeline;

public abstract class AssetHandle(AssetId id)
{
    public AssetId Id { get; } = id;

    private object? value;
    private volatile bool isResolved;


    internal AssetBundle? Owner { get; set; }
    internal bool IsResolved => isResolved;
    internal object? Value => value;

    internal void Resolve(object asset)
    {
        Debug.Assert(isResolved == false);
        value = asset;
        isResolved = true;
    }
}

public sealed class AssetHandle<TValue>(AssetId id) : AssetHandle(id) { }

public sealed class AssetHandleResolver(AssetBundle owner)
{
    public TValue Get<TValue>(AssetHandle<TValue> promise)
    {
        if (promise.Owner != owner)
        {
            throw new InvalidOperationException($"Attempted to resolve a promise that was not owned by the bundle");
        }

        if (promise.Value is TValue value)
        {
            return value;
        }

        throw new Exception($"Internal error: resolved value was not of type {typeof(TValue).Name} but {promise.Value?.GetType().Name ?? "null"}");
    }
}
