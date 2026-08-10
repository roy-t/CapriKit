using CapriKit.Concurrency.Primitives;

namespace CapriKit.AssetPipeline.vNext;

public interface IPromise<TKey>
{
    public TKey Key { get; }
    internal object? Value { get; set; }

    internal int Owner { get; }
}

public sealed class Promise<TKey, TValue>(TKey key, int owner) : IPromise<TKey>
{
    private TValue? _value;

    public TKey Key { get; } = key;
    public int Owner { get; } = owner;

    object? IPromise<TKey>.Value
    {
        get => _value;
        set => _value = (TValue?)value;
    }
}

public sealed class PromiseResolver(int id)
{
    private readonly int Id = id;

    public T Get<T>(IPromise<T> promise)
    {
        if (promise.Owner != Id)
        {
            throw new InvalidOperationException($"Attempted to resolve a promise that was not owned by this resolver");
        }

        if (promise.Value is T value)
        {
            return value;
        }

        throw new Exception($"Internal error: resolved value was not of type {typeof(T).Name} but {promise.Value?.GetType().Name ?? "null"}");
    }
}


public abstract class AssetBundle
{
    private int outstanding;

    internal void OnRequestCompleted(LightweightChannel<AssetBundle> ready)
    {
        if (Interlocked.Decrement(ref outstanding) == 0)
        {
            ready.Write(this);
        }
    }

    internal abstract void Materialize();
}

public sealed internal class AssetBundle<T> : AssetBundle
{
    private readonly TaskCompletionSource<T> Source = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal override void Materialize()
    {
        Source.SetResult()
    }
}
