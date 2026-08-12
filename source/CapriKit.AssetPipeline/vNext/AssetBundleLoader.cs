using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.vNext;

// TODO: this implements the ideas from
// research\AssetPipelineLoadingGroupsV2.md
// but there are still a few open questions


public sealed class AssetBundleBuilder(AssetManager assetManager)
{
    private readonly List<AssetHandle> Handles = [];

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
            handle.Owner = bundle;
        }

        return bundle;
    }
}

public abstract class AssetBundleLoader
{

}

public sealed class AssetBundleLoader<TBundle>(Func<AssetHandleResolver, TBundle> factory, IReadOnlyList<AssetHandle> handles)
    : AssetBundleLoader
    where TBundle : notnull
{
    private TBundle? result;

    // Single threaded!
    // TODO: can we reduce the number of things we need to check each frame?
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        if (result == null)
        {
            foreach (var handle in handles)
            {
                if (!handle.IsResolved)
                {
                    value = default;
                    return false;
                }
            }

            result = factory(new AssetHandleResolver(this));
        }

        value = result;
        return true;
    }

    // TODO: how do we block and wait?
}

public sealed record ExampleAssets(object A, string B)
{
    public static AssetBundleLoader<ExampleAssets> Load(AssetManager assetManager)
    {
        var builder = new AssetBundleBuilder(assetManager);
        var a = builder.Load<string, NoSettings>(new AssetId("key", "path"), default);
        var b = builder.Load<string, NoSettings>(new AssetId("key", "path"), default);

        return builder.Build(r => new ExampleAssets(r.Get(a), r.Get(b)));
    }
}
