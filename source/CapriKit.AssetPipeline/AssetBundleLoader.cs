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
    /// Each asset that needs to loaded returns a handle that can then put used in the lambda
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
            bundle.Add(handle.Id);
            handle.Owner = bundle;
        }

        return bundle;
    }
}

/// <summary>
/// Track the progress of loading the actual bundle and can be used to retrieve the actual bundle when finished.
/// </summary>
public abstract class AssetBundleLoader
{
    private readonly HashSet<AssetId> AssetSet = [];

    internal void Add(AssetId id) => AssetSet.Add(id);
    internal bool IsActive { get; set; } = true;

    public IReadOnlySet<AssetId> Assets => AssetSet;
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
    /// The number of assets that have completed loading, updated every time <see cref="IsReady"/> is called.
    /// </summary>
    public int Loaded => Total - Pending.Count;

    /// <summary>
    /// The latest item that completed loading, updated every time <see cref="IsReady"/> is called.
    /// </summary>
    public AssetId? LastCompletedItem { get; private set; }

    private TBundle? result;
    private bool isReady;

    /// <summary>
    /// Checks whether the bundle finished loading, and if so builds returns it.
    /// Threading: primary thread only.
    /// </summary>
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        if (isReady) { value = result!; return true; }

        for (var i = Pending.Count - 1; i >= 0; i--)
        {
            var handle = Pending[i];
            if (handle.IsResolved)
            {
                LastCompletedItem = handle.Id;
                Pending[i] = Pending[^1];
                Pending.RemoveAt(Pending.Count - 1);
            }
        }

        if (Pending.Count > 0) { value = default; return false; }

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
