using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Use the builder to describe how an asset bundle should be built.
/// </summary>
public sealed class AssetBundleBuilder
{
    private readonly List<AssetHandle> Handles = [];
    private readonly AssetManager assetManager;

    internal AssetBundleBuilder(AssetManager assetManager, string origin)
    {
        this.assetManager = assetManager;
        Origin = origin;
        assetManager.RegisterBuilder(this);
    }

    /// <summary>
    /// The file and line that created this builder, used to name it if its assets are never unloaded.
    /// </summary>
    internal string Origin { get; }

    /// <summary>
    /// The number of assets requested so far.
    /// </summary>
    internal int RequestedAssets => Handles.Count;

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
    /// Creates the bundle that owns the requested assets and that is used to check their loading progress.
    /// Always build a builder that loaded something: the assets of an abandoned builder belong to no bundle
    /// and can therefore never be unloaded.
    /// </summary>
    public AssetBundle<TBundle> Build<TBundle>(Func<AssetHandleResolver, TBundle> factory)
        where TBundle : notnull
    {
        var bundle = new AssetBundle<TBundle>(assetManager, Origin, factory, Handles);
        foreach (var handle in Handles)
        {
            handle.Owner = bundle;
        }

        assetManager.RegisterBundle(this, bundle);
        return bundle;
    }
}

/// <summary>
/// A set of assets that were requested together and that are unloaded together. The bundle holds one lease
/// per handle for as long as it lives, disposing it hands every one of them back. The strongly typed value
/// a bundle produces is only its contents, this object owns the lifetime of the assets in it.
/// </summary>
public abstract class AssetBundle : IDisposable
{
    private readonly List<AssetHandle> HandleList;
    private readonly AssetManager Manager;

    private protected AssetBundle(AssetManager manager, string origin, IReadOnlyList<AssetHandle> handles)
    {
        Manager = manager;
        Origin = origin;

        // Copied on purpose: the builder that handed us this list stays usable and may load more assets
        // into it, those belong to whatever bundle is built next and not to this one.
        HandleList = [.. handles];
    }

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
    /// Unloads this bundle, see <see cref="AssetManager.Unload(AssetBundle)"/>.
    /// Unloading a bundle twice is safe, the second call does nothing.
    /// </summary>
    public void Dispose() => Manager.Unload(this);
}

/// <inheritdoc cref="AssetBundle"/>
public sealed class AssetBundle<TBundle> : AssetBundle
    where TBundle : notnull
{
    private readonly Func<AssetHandleResolver, TBundle> Factory;
    private readonly List<AssetHandle> Pending;

    private TBundle? result;
    private bool isReady;

    internal AssetBundle(AssetManager manager, string origin, Func<AssetHandleResolver, TBundle> factory, IReadOnlyList<AssetHandle> handles)
        : base(manager, origin, handles)
    {
        Factory = factory;
        Pending = [.. handles];
        Total = handles.Count;
    }

    /// <summary>
    /// The number of assets in this bundle.
    /// </summary>
    public int Total { get; }

    /// <summary>
    /// The number of assets that have arrived, successfully or not, updated every time
    /// <see cref="IsReady"/> is called.
    /// </summary>
    public int Loaded => Total - Pending.Count;

    /// <summary>
    /// The latest item that completed loading, updated every time <see cref="IsReady"/> is called.
    /// </summary>
    public AssetId? LastCompletedItem { get; private set; }

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
        ObjectDisposedException.ThrowIf(!IsActive, this);

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

        result = value = Factory(new AssetHandleResolver(this));
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
