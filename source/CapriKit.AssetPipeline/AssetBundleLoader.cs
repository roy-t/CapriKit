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

    // TODO: how can we put a sort of progress bar and progress information on this thing?
}
