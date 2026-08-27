using CapriKit.Concurrency.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.Playground;


public record Example(string SomeAsset)
{
    public static void ExampleLoad(AssetManager manager)
    {
        var builder = new AssetBundleBuilder<Example>(manager);
        var handle = builder.Request<string, NoSettings>(new AssetId("text.txt"), default);
        using var bundle = builder.Build(r => new Example(r.Get(handle)));

        while (true)
        {
            if (bundle.IsReady(out var content))
            {
                // We can now access our bundle contents
                return;
            }
        }
    }
}


public readonly record struct AssetHandle<TValue>(AssetId Id, Guid Owner);

public sealed class AssetBundleBuilder<TContents>(AssetManager assetManager)
    where TContents : class
{
    private readonly HashSet<AssetId> Requested = [];
    private readonly List<Action<AssetBundle<TContents>>> Requests = [];
    private readonly Guid Id = Guid.NewGuid();

    public AssetHandle<TAsset> Request<TAsset, TSettings>(AssetId id, TSettings settings)
       where TAsset : class
    {
        if (!Requested.Add(id)) { throw new Exception($"You cannot request the same asset twice for the same bundle: {id}."); }
        Requests.Add(b => assetManager.Load<TAsset, TSettings>(id, settings, b));
        return new AssetHandle<TAsset>(id, Id);
    }

    public AssetBundle<TContents> Build(Func<AssetBundle<TContents>.AssetResolver, TContents> resolver)
    {
        var bundle = new AssetBundle<TContents>(Id, assetManager, Requested, resolver);
        foreach (var request in Requests)
        {
            request(bundle);
        }

        return bundle;
    }
}

public interface IAssetRequester
{
    /// <summary>
    /// Takes ownership of the asset (or failure).
    /// </summary>
    /// <returns>True if ownership is accepted. False if the requester stopped accepting new inputs.</returns>
    public bool Accept(AssetId id, JobResult<object> result);
}

public sealed class AssetBundle<TContent> : IAssetRequester, IDisposable
    where TContent : class
{
    private readonly Guid Id;
    private readonly AssetManager AssetManager;
    private readonly Func<AssetResolver, TContent> Resolver;
    private readonly Lock Lock = new();
    private readonly IReadOnlySet<AssetId> Requested;
    private readonly Dictionary<AssetId, JobResult<object>> Received;
    private bool isReady;
    private TContent? content;

    internal AssetBundle(Guid id, AssetManager assetManager, IReadOnlySet<AssetId> requested, Func<AssetResolver, TContent> resolver)
    {
        Id = id;
        AssetManager = assetManager;
        Requested = requested;
        Resolver = resolver;
        Received = [];
    }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Threading: unsafe, only one thread can access this method at the same time.
    /// </summary>    
    public bool Accept(AssetId id, JobResult<object> result)
    {
        if (IsDisposed) { return false; }
        Received[id] = result;
        return true;
    }

    internal TAsset Get<TAsset>(AssetHandle<TAsset> handle)
        where TAsset : class
    {
        if (handle.Owner != Id)
        {
            throw new InvalidOperationException("Attempted to resolve a handle that was not created for this bundle");
        }

        return ((Asset<TAsset>)Received[handle.Id].GetOrThrow()).Value;
    }

    /// <summary>
    /// Threading: unsafe, only one thread can access this method at the same time.
    /// </summary>
    public bool IsReady([NotNullWhen(true)] out TContent? content)
    {
        if (isReady)
        {
            content = this.content!;
            return true;
        }

        if (Resolver == null)
        {
            throw new InvalidOperationException("Call Build before calling IsReady");
        }


        if (Received.Count == Requested.Count)
        {
            content = Resolver(new AssetResolver(this));
            isReady = true;
            return true;
        }

        content = default;
        return false;
    }

    /// <summary>
    /// Threading: unsafe, only one thread can access this method at the same time.
    /// </summary>    
    public void Dispose()
    {
        if (IsDisposed) { return; }

        foreach (var kv in Received)
        {
            // Ignore any assets that failed to load during disposal
            kv.Value.Match((id, asset) => AssetManager.Unload(id), (id, ex) => { });
        }
        Received.Clear();
        IsDisposed = true;
    }

    public sealed class AssetResolver
    {
        private readonly AssetBundle<TContent> Owner;
        internal AssetResolver(AssetBundle<TContent> owner)
        {
            Owner = owner;
        }

        public TAsset Get<TAsset>(AssetHandle<TAsset> handle)
            where TAsset : class => Owner.Get(handle);
    }
}
