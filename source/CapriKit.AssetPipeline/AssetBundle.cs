using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// A set of assets that are loaded together and unloaded together. The bundle holds one lease per asset it
/// loaded for as long as it lives, disposing it hands every one of them back. Use <see cref="Build"/> to
/// describe the strongly typed value those assets add up to, the bundle itself owns their lifetime.
/// Threading: create, load and build from one thread, usually the primary one.
/// </summary>
public sealed class AssetBundle : IDisposable
{
    private readonly List<AssetHandle> HandleList = [];
    private readonly AssetManager Manager;

    internal AssetBundle(AssetManager manager, string origin)
    {
        Manager = manager;
        Origin = origin;
    }

    /// <summary>
    /// The number of assets in this bundle.
    /// </summary>
    public int Total => HandleList.Count;

    /// <summary>
    /// The number of assets that have arrived, successfully or not, updated every time
    /// <see cref="AssetBundleLoader{TBundle}.IsReady"/> is called.
    /// </summary>
    public int Loaded { get; private set; }

    /// <summary>
    /// The last asset that had arrived the last time <see cref="AssetBundleLoader{TBundle}.IsReady"/> was
    /// called, in the order the assets were requested. Meant to put a name on a loading screen, not to
    /// report the exact order in which assets finished.
    /// </summary>
    public AssetId? LastCompletedItem { get; private set; }

    /// <summary>
    /// Every handle in this bundle, failed ones included, kept for as long as the bundle lives. Unloading
    /// counts handles rather than distinct assets because the pool hands out one lease per resolved handle:
    /// an asset this bundle asked for twice holds two leases, and one that failed to load holds none.
    /// </summary>
    internal IReadOnlyList<AssetHandle> Handles => HandleList;

    internal bool IsActive { get; set; } = true;

    /// <summary>
    /// The file and line that created this bundle, used to name it if it is never unloaded.
    /// </summary>
    internal string Origin { get; }

    /// <summary>
    /// Starts building (if needed) and loading an asset, and adds it to this bundle. The handle it returns
    /// is used in the lambda for <see cref="Build{TBundle}(Func{AssetHandleResolver, TBundle})"/> to
    /// describe how the assets in this bundle add up to one strongly typed value. Load everything the
    /// bundle needs before building it, an asset loaded afterwards still belongs to the bundle but the
    /// loaders that were already built cannot see it.
    /// </summary>
    public AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var handle = Manager.Load<TAsset, TSettings>(id, settings);
        handle.Owner = this;
        HandleList.Add(handle);
        return handle;
    }

    /// <inheritdoc cref="Load{TAsset, TSettings}(AssetId, TSettings)"/>
    public AssetHandle<TAsset> Load<TAsset>(AssetId id)
        where TAsset : class
        => Load<TAsset, NoSettings>(id, default);

    /// <summary>
    /// Creates the loader that reports the progress of this bundle and that builds the strongly typed value
    /// its assets add up to. The loader borrows the bundle, it does not own it: this bundle stays the thing
    /// that has to be unloaded, which is why building twice is harmless.
    /// </summary>
    public AssetBundleLoader<TBundle> Build<TBundle>(Func<AssetHandleResolver, TBundle> factory)
        where TBundle : notnull
        => new(this, factory);

    /// <summary>
    /// Unloads this bundle, see <see cref="AssetManager.Unload(AssetBundle)"/>.
    /// Unloading a bundle twice is safe, the second call does nothing.
    /// </summary>
    public void Dispose() => Manager.Unload(this);

    /// <summary>
    /// Updates <see cref="Loaded"/> and <see cref="LastCompletedItem"/> and reports whether every asset in
    /// the bundle arrived, successfully or not. Walks all handles rather than keeping a list of the pending
    /// ones: a bundle holds a handful of assets, and one list is easier to reason about than two that have
    /// to agree with each other.
    /// </summary>
    internal bool AllAssetsArrived()
    {
        var arrived = 0;
        foreach (var handle in HandleList)
        {
            // An asset that failed has arrived too, it just arrived as an error instead of as a value.
            if (handle.IsCompleted)
            {
                arrived++;
                LastCompletedItem = handle.Id;
            }
        }

        Loaded = arrived;
        return arrived == HandleList.Count;
    }

    /// <summary>
    /// Reports the assets in this bundle that could not be built or loaded, in the order they were
    /// requested. Only call this once <see cref="AllAssetsArrived"/> is true: until then an asset might
    /// still take a lease, and reporting a failure before that would leave it unaccounted for.
    /// </summary>
    internal void ThrowOnFailedAssets()
    {
        List<AssetLoadException>? failures = null;
        foreach (var handle in HandleList)
        {
            if (handle.Error is not null)
            {
                (failures ??= []).Add(handle.Error);
            }
        }

        if (failures is null) { return; }
        if (failures.Count == 1) { throw failures[0]; }

        throw new AggregateException(failures);
    }
}

/// <summary>
/// Tracks the loading progress of an <see cref="AssetBundle"/> and builds the strongly typed value its
/// assets add up to once they all arrived. Purely a view on the bundle: unloading goes through the bundle,
/// so a loader that is dropped costs nothing.
/// </summary>
public sealed class AssetBundleLoader<TBundle>
    where TBundle : notnull
{
    private readonly AssetBundle Bundle;
    private readonly Func<AssetHandleResolver, TBundle> Factory;

    private TBundle? result;
    private bool isReady;

    internal AssetBundleLoader(AssetBundle bundle, Func<AssetHandleResolver, TBundle> factory)
    {
        Bundle = bundle;
        Factory = factory;
    }

    /// <inheritdoc cref="AssetBundle.Total"/>
    public int Total => Bundle.Total;

    /// <inheritdoc cref="AssetBundle.Loaded"/>
    public int Loaded => Bundle.Loaded;

    /// <inheritdoc cref="AssetBundle.LastCompletedItem"/>
    public AssetId? LastCompletedItem => Bundle.LastCompletedItem;

    /// <summary>
    /// Checks whether the bundle finished loading, and if so builds and returns it. The result is
    /// cached so calling IsReady multiple times after loading finished is OK.
    /// Throws a <see cref="AssetLoadException"/> when an asset in this bundle could not be built or
    /// loaded. Throws an <see cref="AggregateException"/> if multiple assets failed to load.
    /// Throws an <see cref="ObjectDisposedException"/> once the bundle has been unloaded.
    /// </summary>
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        // An unloaded bundle gave its leases back, so the assets it would hand out may already be disposed.
        ObjectDisposedException.ThrowIf(!Bundle.IsActive, Bundle);

        if (isReady) { value = result!; return true; }

        if (!Bundle.AllAssetsArrived()) { value = default; return false; }

        // Everything arrived, so every lease this bundle will ever hold is settled and it is safe to report
        // a failure. isReady stays false, so every later call throws again.
        Bundle.ThrowOnFailedAssets();

        result = value = Factory(new AssetHandleResolver(Bundle));
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
