using System.Diagnostics;

namespace CapriKit.AssetPipeline.vNext;

public abstract class AssetHandle()
{
    internal protected bool isResolved;
    internal protected object? value;

    internal AssetBundleLoader? Owner { get; set; }
    internal bool IsResolved => isResolved;
    internal object? Value => value;

    internal void Resolve(object asset)
    {
        Debug.Assert(isResolved == false);
        value = asset;
        isResolved = true;
    }
}

public sealed class AssetHandle<TValue> : AssetHandle
{

}

public sealed class AssetHandleResolver(AssetBundleLoader owner)
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
