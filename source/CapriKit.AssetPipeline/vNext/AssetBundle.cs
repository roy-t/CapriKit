using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.vNext;

// TODO: this implements the ideas from
// research\AssetPipelineLoadingGroupsV2.md
// but there are still a few open questions


public sealed class AssetBundleBuilder(AssetManager assetManager)
{
    private readonly List<Promise> Promises = [];

    public Promise<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var promise = assetManager.Load<TAsset, TSettings>(id, settings);
        Promises.Add(promise);
        return promise;
    }

    public AssetBundle<TBundle> Build<TBundle>(Func<PromiseResolver, TBundle> factory)
        where TBundle : notnull
    {
        var bundle = new AssetBundle<TBundle>(factory, Promises);
        foreach (var promise in Promises)
        {
            promise.Owner = bundle;
        }

        return bundle;
    }
}

public abstract class AssetBundle
{

}

public sealed class AssetBundle<TBundle>(Func<PromiseResolver, TBundle> factory, IReadOnlyList<Promise> promises)
    : AssetBundle
    where TBundle : notnull
{
    private TBundle? result;

    // Single threaded!
    // TODO: can we reduce the number of things we need to check each frame?
    public bool IsReady([NotNullWhen(true)] out TBundle? value)
    {
        if (result == null)
        {
            foreach (var promise in promises)
            {
                if (!promise.IsResolved)
                {
                    value = default;
                    return false;
                }
            }

            result = factory(new PromiseResolver(this));
        }

        value = result;
        return true;
    }

    // TODO: how do we block and wait?
}

public sealed record ExampleAssets(object A, string B)
{
    public static AssetBundle<ExampleAssets> Define(AssetManager assetManager)
    {
        var builder = new AssetBundleBuilder(assetManager);
        var a = builder.Load<string, NoSettings>(new AssetId("key", "path"), default);
        var b = builder.Load<string, NoSettings>(new AssetId("key", "path"), default);

        return builder.Build(r => new ExampleAssets(r.Get(a), r.Get(b)));
    }
}
