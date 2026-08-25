using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Use the builder to describe how an asset bundle should be built.
/// </summary>
public sealed class AssetBundleBuilder
{
    private readonly List<AssetHandle> Handles = [];
    private readonly AssetManager assetManager;

    internal AssetBundleBuilder(AssetManager assetManager)
    {
        this.assetManager = assetManager;
    }

    /// <summary>
    /// Each asset that needs to load returns a handle that can then be used in the lambda
    /// for <see cref="Build{TBundle}(Func{AssetHandleResolver, TBundle})"/> to describe how
    /// the asset bundle should actually be built.
    /// </summary>
    public AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var handle = assetManager.Load<TAsset, TSettings>(id, settings);
        Handles.Add(handle);
        return handle;
    }

    /// <inheritdoc cref="Load{TAsset, TSettings}(AssetId, TSettings)"/>
    public AssetHandle<TAsset> Load<TAsset>(AssetId id)
        where TAsset : class
        => Load<TAsset, NoSettings>(id, default);

    /// <summary>
    /// Create a loader that is used to check the loading progress of the bundle.
    /// </summary>
    public AssetBundleLoader<TBundle> Build<TBundle>(Func<AssetHandleResolver, TBundle> factory)
        where TBundle : notnull
    {
        var bundle = new AssetBundleLoader<TBundle>(factory, Handles);
        foreach (var handle in Handles)
        {
            bundle.Add(handle);
            handle.Owner = bundle;
        }

        return bundle;
    }
}

/// <summary>
/// Tracks the progress of loading the actual bundle and can be used to retrieve the actual bundle when finished.
/// </summary>
public abstract class AssetBundleLoader
{
    private readonly List<AssetHandle> HandleList = [];

    internal void Add(AssetHandle handle) => HandleList.Add(handle);

    /// <summary>
    /// Every handle in this bundle, failed ones included, kept for as long as the bundle lives. Unloading
    /// counts handles rather than distinct assets because the pool hands out one lease per resolved handle:
    /// an asset this bundle asked for twice holds two leases, and one that failed to load holds none.
    /// </summary>
    internal IReadOnlyList<AssetHandle> Handles => HandleList;

    internal bool IsActive { get; set; } = true;
}

/// <inheritdoc cref="AssetBundleLoader"/>
public sealed class AssetBundleLoader<TBundle>(Func<AssetHandleResolver, TBundle> factory, IReadOnlyList<AssetHandle> handles)
    : AssetBundleLoader
    where TBundle : notnull
{
    private readonly List<AssetHandle> Pending = [.. handles];

    /// <summary>
    /// The number of assets in this bundle.
    /// </summary>
    public int Total { get; } = handles.Count;

    /// <summary>
    /// The number of assets that have arrived, successfully or not, updated every time
    /// <see cref="IsReady"/> is called.
    /// </summary>
    public int Loaded => Total - Pending.Count;

    /// <summary>
    /// The latest item that completed loading, updated every time <see cref="IsReady"/> is called.
    /// </summary>
    public AssetId? LastCompletedItem { get; private set; }

    private TBundle? result;
    private bool isReady;

    /// <summary>
    /// Checks whether the bundle finished loading, and if so builds and returns it. The result is
    /// cached so calling IsReady multiple times after loading finished is OK.
    /// Throws a <see cref="AssetLoadException"/> when an asset in this bundle could not be built or
    /// loaded. Throws an <see cref="AggregateException"/> if multiple assets failed to load.
    /// </summary>
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        if (isReady) { value = result!; return true; }

        for (var i = Pending.Count - 1; i >= 0; i--)
        {
            var handle = Pending[i];
            if (!handle.IsCompleted) { continue; }

            // An asset that failed has arrived too, it just arrived as an error instead of as a value.
            LastCompletedItem = handle.Id;
            Pending[i] = Pending[^1];
            Pending.RemoveAt(Pending.Count - 1);
        }

        if (Pending.Count > 0) { value = default; return false; }

        // Everything arrived, so every lease this bundle will ever hold is settled and it is safe to report
        // a failure. Handles keep their own error, so this needs no bookkeeping of its own and reports in
        // the order the assets were requested. isReady stays false, so every later call throws again.
        List<AssetLoadException>? failures = null;
        foreach (var handle in Handles)
        {
            if (handle.Error is not null)
            {
                (failures ??= []).Add(handle.Error);
            }
        }

        if (failures != null)
        {
            if (failures.Count == 1) { throw failures[0]; }
            throw new AggregateException(failures);
        }

        result = value = factory(new AssetHandleResolver(this));
        isReady = true;
        return true;
    }

    /// <summary>
    /// Busy waits until the bundle finishes loading, then builds and returns it.
    /// Threading: primary thread only.
    /// </summary>
    public TBundle WaitUntilReady()
    {
        var wait = new SpinWait();
        while (true)
        {
            if (IsReady(out var bundle)) { return bundle; }
            wait.SpinOnce();
        }
    }
}
