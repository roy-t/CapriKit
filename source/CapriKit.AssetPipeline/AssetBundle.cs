using CapriKit.Concurrency.Primitives;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CapriKit.AssetPipeline;

/// <summary>
/// Something the <see cref="AssetManager"/> can hand a loaded asset to.
/// Threading: the manager only calls this from the main thread.
/// </summary>
internal interface IAssetRequester
{
    /// <summary>
    /// Takes ownership of one finished asset, successful or failed. Accepting a successful result takes a
    /// lease, which the manager records against this requester and hands back when it unloads. Refusing one
    /// leaves the lease with the manager, which returns it instead.
    /// </summary>
    /// <returns>True if ownership is accepted, false if this requester stopped accepting results.</returns>
    bool Accept(AssetId id, JobResult<object> result);
}

/// <summary>
/// Describes the assets a bundle needs. Use <see cref="Build"/> to create the actual bundle and start the loading process.
/// Threading: unsafe, only one thread can access the same builder at the same time.
/// </summary>
public sealed class AssetBundleBuilder<TContents>(AssetManager manager, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    where TContents : class
{
    private readonly AssetManager Manager = manager;
    private readonly HashSet<AssetId> Requested = [];
    private readonly List<Action<AssetBundle<TContents>>> Requests = [];
    private readonly Guid BundleId = Guid.NewGuid();
    private readonly string Origin = $"{Path.GetFileName(file.AsSpan())}:{line}";
    private bool isBuilt;

    /// <summary>
    /// Records that the bundle needs this asset and hands back the handle used to read it once the bundle is
    /// ready. Requesting the same asset twice throws: a bundle holds exactly one lease per asset, so the
    /// second request would be a lease that nobody ever gives back.
    /// </summary>
    public AssetHandle<TAsset> Request<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        ThrowIfBuilt();

        if (!Requested.Add(id))
        {
            throw new InvalidOperationException($"You cannot request the same asset twice for the same bundle: {id}.");
        }

        Requests.Add(bundle => Manager.Load<TAsset, TSettings>(id, settings, bundle));

        return new AssetHandle<TAsset>(id, BundleId);
    }

    /// <inheritdoc cref="Request{TAsset, TSettings}(AssetId, TSettings)"/>
    public AssetHandle<TAsset> Request<TAsset>(AssetId id)
        where TAsset : class
        => Request<TAsset, NoSettings>(id, default);

    /// <summary>
    /// Creates the bundle and starts building and loading the requested content. A bundle leases
    /// the assets from the asset system and by disposing it the leases are returned.
    /// </summary>
    public AssetBundle<TContents> Build(Func<AssetBundle<TContents>.AssetResolver, TContents> factory)
    {
        ThrowIfBuilt();
        isBuilt = true;

        var bundle = new AssetBundle<TContents>(BundleId, Manager, Requested, factory);
        Manager.Register(bundle, Origin);

        try
        {
            foreach (var request in Requests)
            {
                request(bundle);
            }
        }
        catch
        {
            // The bundle never reaches the caller
            bundle.Dispose();
            throw;
        }

        return bundle;
    }

    private void ThrowIfBuilt()
    {
        if (isBuilt)
        {
            throw new InvalidOperationException("This builder already built its bundle, use a new builder for another one.");
        }
    }
}

/// <summary>
/// A set of assets wrapped in a bundle to track loading and facilitate unloading. An asset bundle must be
/// disposed off when the assets are no longer used so that they are cleaned-up correctly.
/// Create an asset bundle using an <see cref="AssetBundleBuilder{TContents}"/>.
/// Threading: use one bundle from one thread, the same one that calls <see cref="AssetManager.Update"/>.
/// </summary>
public sealed class AssetBundle<TContent> : IAssetRequester, IDisposable
    where TContent : class
{
    private readonly Guid Id;
    private readonly AssetManager Manager;
    private readonly IReadOnlySet<AssetId> Requested;
    private readonly Dictionary<AssetId, JobResult<object>> Received;
    private readonly Func<AssetResolver, TContent> Factory;
    private TContent? contents;
    private bool isReady;

    internal AssetBundle(Guid id, AssetManager manager, IReadOnlySet<AssetId> requested, Func<AssetResolver, TContent> factory)
    {
        Id = id;
        Manager = manager;
        Requested = requested;
        Factory = factory;
        Received = [];
    }

    public bool IsDisposed { get; private set; }

    /// <summary>The number of assets in this bundle.</summary>
    public int Total => Requested.Count;

    /// <summary>The number of assets that arrived, whether they loaded successfully or failed.</summary>
    public int Loaded => Received.Count;

    /// <summary>
    /// The asset that arrived most recently, meant to put a name on a loading screen rather than to report
    /// the exact order in which assets finished. Null until the first asset arrives.
    /// </summary>
    public AssetId? LastCompletedItem { get; private set; }

    /// <summary>
    /// Reports whether every asset arrived, and if so builds and returns the value they add up to. The
    /// result is cached.
    /// Throws an <see cref="AssetLoadException"/> when one asset in this bundle could not be built or
    /// loaded, and an <see cref="AggregateException"/> when several could not.
    /// Throws an <see cref="ObjectDisposedException"/> once this bundle has been disposed.
    /// </summary>
    public bool IsReady([NotNullWhen(true)] out TContent? contents)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (isReady)
        {
            contents = this.contents!;
            return true;
        }

        if (Received.Count < Requested.Count)
        {
            contents = default;
            return false;
        }

        ThrowOnFailedAssets();

        this.contents = contents = Factory(new AssetResolver(this));
        isReady = true;
        return true;
    }

    /// <summary>
    /// Hands every asset in this bundle back to the asset manager. Unloading a bundle that failed to load
    /// works too: the assets that did load are returned, and the ones that failed never took anything that
    /// needs returning. So does unloading one that is still loading, the assets that are still on their way
    /// are returned as soon as they arrive. Disposing twice is safe, the second call does nothing.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed) { return; }

        // Set before anything else: an asset that finishes after this point has to find a bundle that
        // refuses it, whether or not unloading below succeeds.
        IsDisposed = true;

        // The manager recorded every lease it handed us, so it needs nothing from us but our identity.
        Received.Clear();
        Manager.Unload(this);
    }

    bool IAssetRequester.Accept(AssetId id, JobResult<object> result)
    {
        if (IsDisposed) { return false; }

        Received[id] = result;
        LastCompletedItem = id;
        return true;
    }

    private TAsset Get<TAsset>(AssetHandle<TAsset> handle)
        where TAsset : class
    {
        if (handle.Owner != Id)
        {
            throw new InvalidOperationException($"Attempted to resolve the handle for {handle.Id}, which was not created for this bundle");
        }

        return (TAsset)Received[handle.Id].GetOrThrow();
    }

    /// <summary>
    /// Reports every asset in this bundle that could not be built or loaded.
    /// </summary>
    private void ThrowOnFailedAssets()
    {
        List<AssetLoadException>? failures = null;
        foreach (var (id, result) in Received)
        {
            if (result.IsSuccess) { continue; }

            result.Match(
                static (_, _) => { },
                (_, error) => (failures ??= []).Add(AsLoadException(id, error.SourceException)));
        }

        if (failures is null) { return; }
        if (failures.Count == 1) { throw failures[0]; }

        throw new AggregateException(failures);
    }

    // The manager wraps a failure before it hands it over, the fallback is only here so that a requester
    // filled in by anything else still reports which asset the failure belongs to.
    private static AssetLoadException AsLoadException(AssetId id, Exception error)
        => error as AssetLoadException ?? new AssetLoadException(id, error);


    /// <summary>
    /// Allows looking up loaded assets by their handle.
    /// </summary>
    public sealed class AssetResolver
    {
        private readonly AssetBundle<TContent> Owner;

        internal AssetResolver(AssetBundle<TContent> owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Provide the handle returned by <seealso cref="AssetBundleBuilder{TContents}.Request{TAsset, TSettings}(AssetId, TSettings)"/> to obtain the loaded asset.
        /// You can only use handles created by the builder that built the asset bundle.
        /// </summary>        
        public TAsset Get<TAsset>(AssetHandle<TAsset> handle)
            where TAsset : class => Owner.Get(handle);
    }
}
