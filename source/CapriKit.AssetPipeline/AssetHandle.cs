using System.Diagnostics;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Represents an asset that is in the progress of loading.
/// </summary>
public abstract class AssetHandle(AssetId id)
{
    public AssetId Id { get; } = id;

    private object? value;
    private AssetLoadException? error;
    private volatile bool isResolved;


    internal AssetBundleLoader? Owner { get; set; }

    /// <summary>True once the asset arrived, whether it loaded successfully or failed.</summary>
    internal bool IsCompleted => isResolved;

    /// <summary>The failure that stopped this asset from loading, null while loading and after success.</summary>
    internal AssetLoadException? Error => error;

    /// <summary>True once the asset arrived successfully, which is also when it holds a lease on the pool.</summary>
    internal bool IsLoaded => IsCompleted && error is null;

    internal object? Value => value;

    internal void Resolve(object asset)
    {
        Debug.Assert(isResolved == false);
        value = asset;
        isResolved = true;
    }

    /// <summary>
    /// Hands this asset's failure to whoever is waiting for it. The volatile write to isResolved happens
    /// last so a reader that sees the handle complete also sees the error, which is the same ordering
    /// <see cref="Resolve"/> relies on for its value.
    /// </summary>
    internal void Fail(AssetLoadException exception)
    {
        Debug.Assert(isResolved == false);
        error = exception;
        isResolved = true;
    }
}

/// <inheritdoc cref="AssetHandle"/>
public sealed class AssetHandle<TValue>(AssetId id) : AssetHandle(id) { }

/// <summary>
/// Helper class for resolving loaded assets from their asset handle.
/// </summary>
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
