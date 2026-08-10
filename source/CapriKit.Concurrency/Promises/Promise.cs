namespace CapriKit.Concurrency.Promises;

public abstract class Promise
{
    internal Promise(int owner)
    {
        Owner = owner;
    }

    internal int Owner { get; }
    internal object? Value { get; set; }
}

public sealed class Promise<TValue>(int owner) : Promise(owner);

public sealed class PromiseResolver(int id)
{
    private readonly int Id = id;

    public TValue Get<TValue>(Promise<TValue> promise)
    {
        if (promise.Owner != Id)
        {
            throw new InvalidOperationException($"Attempted to resolve a promise that was not owned by this resolver");
        }

        if (promise.Value is TValue value)
        {
            return value;
        }

        throw new Exception($"Internal error: resolved value was not of type {typeof(TValue).Name} but {promise.Value?.GetType().Name ?? "null"}");
    }
}
