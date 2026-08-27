using CapriKit.Concurrency.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.Playground;


public record Example(string SomeAsset)
{
    public static void ExampleLoad(AssetManager manager)
    {
        using var bundle = new AssetBundle<Example>(manager);
        var handle = bundle.Request<string, NoSettings>(new AssetId("text.txt"), default);
        bundle.Build(r => new Example(r.Get(handle)));

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


public sealed record AssetHandle<TValue>(AssetId Id);


public sealed class AssetBundle<TContent>(AssetManager assetManager) : IDisposable
    where TContent : class
{
    private readonly AssetManager AssetManager = assetManager;
    private Func<AssetResolver, TContent>? resolver;
    private readonly Lock Lock = new();
    private readonly HashSet<AssetId> Requested = [];
    private readonly Dictionary<AssetId, JobResult<Asset>> Received = [];
    private bool isReady;
    private TContent? content;
    public bool IsDisposed { get; private set; }

    public void Build(Func<AssetResolver, TContent> resolver)
    {
        this.resolver = resolver;
    }

    /// <summary>
    /// Threading: thread-safe
    /// </summary>    
    internal bool Accept(AssetId id, JobResult<Asset> result)
    {
        lock (Lock)
        {
            if (IsDisposed) { return false; }
            Received[id] = result;
            return true;
        }
    }

    internal TAsset Get<TAsset>(AssetHandle<TAsset> handle)
        where TAsset : class
    {
        if (handle.Owner != this)
        {
            throw new InvalidOperationException("Attempted to resolve a handle that was not created by this bundle");
        }

        return ((Asset<TAsset>)Received[handle.Id].GetOrThrow()).Value;
    }

    /// <summary>
    /// Threading: unsafe, only one thread can access this method at the same time, but it is safe for other threads
    /// to access the other methods on this type.
    /// </summary>    
    public AssetHandle<TAsset> Request<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        if (!Requested.Add(id)) { throw new Exception($"You cannot request the same asset twice from the same bundle: {id}."); }
        var handle = AssetManager.Load<TAsset, TSettings>(id, settings);
        handle.Owner = this;
        return handle;
    }

    /// <summary>
    /// Threading: unsafe, only one thread can access this method at the same time, but it is safe for other threads
    /// to access the other methods on this type.
    /// </summary> 
    public bool IsReady([NotNullWhen(true)] out TContent? content)
    {
        if (isReady)
        {
            content = this.content!;
            return true;
        }

        if (resolver == null)
        {
            throw new InvalidOperationException("Call Build before calling IsReady");
        }


        if (Received.Count == Requested.Count)
        {
            content = resolver(new AssetResolver(this));
            isReady = true;
            return true;
        }

        content = default;
        return false;
    }

    public void Dispose()
    {
        lock (Lock)
        {
            if (IsDisposed) { return; }

            foreach (var kv in Received)
            {
                AssetManager.Unload(kv.Key);
            }
            Received.Clear();
            IsDisposed = true;
        }
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
