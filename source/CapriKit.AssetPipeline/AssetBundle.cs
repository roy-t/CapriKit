using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline;

public sealed class AssetBundleBuilder
{
    private readonly List<AssetHandle> Handles = [];
    private readonly AssetManager assetManager;

    internal AssetBundleBuilder(AssetManager assetManager)
    {
        this.assetManager = assetManager;
    }

    public AssetHandle<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var handle = assetManager.Load<TAsset, TSettings>(id, settings);
        Handles.Add(handle);
        return handle;
    }

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

public abstract class AssetBundle
{
    private readonly HashSet<AssetId> AssetSet = [];

    internal void Add(AssetId id) => AssetSet.Add(id);
    internal bool IsActive { get; set; } = true;

    public IReadOnlySet<AssetId> Assets => AssetSet;
}

public sealed class AssetBundleLoader<TBundle>(Func<AssetHandleResolver, TBundle> factory, IReadOnlyList<AssetHandle> handles)
    : AssetBundle
    where TBundle : notnull
{
    private bool isReady;
    private TBundle? result;

    // Single threaded!
    // TODO: can we reduce the number of things we need to check each frame?
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        if (isReady)
        {
            value = result!;
            return true;
        }

        foreach (var handle in handles)
        {
            if (!handle.IsResolved)
            {
                value = default;
                return false;
            }
        }

        result = factory(new AssetHandleResolver(this));
        isReady = true;

        value = result;
        return true;
    }

    // TODO: Add a method to block and wait without eating all the CPU.
}
