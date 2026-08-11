namespace CapriKit.AssetPipeline.vNext;

public abstract class Promise
{
    internal object? Value { get; set; }
}

public sealed class Promise<TValue> : Promise;

public sealed class PromiseResolver
{
    public TValue Get<TValue>(Promise<TValue> promise)
    {
        // TODO: can we somehow double check that this resolver is able to resolve the promise?
        //if (promise.Owner != Id)
        //{
        //    throw new InvalidOperationException($"Attempted to resolve a promise that was not owned by this resolver");
        //}

        if (promise.Value is TValue value)
        {
            return value;
        }

        throw new Exception($"Internal error: resolved value was not of type {typeof(TValue).Name} but {promise.Value?.GetType().Name ?? "null"}");
    }
}
