using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.vNext;

// TODO: this and the promises in CapriKit.Concurrency implement the ideas from
// research\AssetPipelineLoadingGroups.md
// but there are still a few open questions
// - I need a blocking one so that all systems can start (Bootstrap), is that await? In CapriKit.Test.Tool I seem to be able to avoid that
// - I need a non-blocking one for all the other assets for stuff that should happen during loading screens what mechanism to use?
//   - LightWeightChannel to the rescue and then swapping the entire 'loading scene' with the new scene?
// - When does the actual loading start and how do we register for it? See AssetManager.Bundle
// - How/when do we set the owner of the promise for the extra check?

public abstract class AssetBundle(int assets)
{
    protected readonly CountdownEvent Outstanding = new(assets);
    internal void OnRequestCompleted()
    {
        Outstanding.Signal();
    }
}

public sealed class AssetBundle<T> : AssetBundle
    where T : notnull
{
    private readonly Func<PromiseResolver, T> Resolver;

    internal AssetBundle(int assets, Func<PromiseResolver, T> resolver)
        : base(assets)
    {
        Resolver = resolver;
    }

    public T Wait(CancellationToken cancellationToken = default)
    {
        Outstanding.Wait(cancellationToken);
        return Resolver(new PromiseResolver());
    }

    public bool Check([NotNullWhen(true)] out T? value)
    {
        if (Outstanding.IsSet)
        {
            value = Wait();
            return true;
        }
        value = default;
        return false;
    }
}

public sealed record ExampleAssets(object A, string B)
{
    public static AssetBundle<ExampleAssets> Define(AssetManager assetManager)
    {
        var a = assetManager.Load<string, NoSettings>(new AssetId("key", "path"), default);
        var b = assetManager.Load<string, NoSettings>(new AssetId("key", "path"), default);

        return assetManager.Bundle(r => new ExampleAssets(r.Get(a), r.Get(b)));
    }
}
