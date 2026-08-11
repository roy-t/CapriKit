using System.Diagnostics;

namespace CapriKit.AssetPipeline.vNext;

public abstract class Promise()
{
    internal AssetBundle? Owner { get; set; }
    internal bool IsResolved { get; private set; }
    internal object? Value { get; private set; }

    internal void Resolve(object value)
    {
        Debug.Assert(IsResolved == false);

        Value = value;
        IsResolved = true;
    }
}

public sealed class Promise<TValue> : Promise;

public sealed class PromiseResolver(AssetBundle owner)
{
    public TValue Get<TValue>(Promise<TValue> promise)
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
