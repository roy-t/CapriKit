using CapriKit.Concurrency.Primitives;
using CapriKit.Concurrency.Promises;
using CapriKit.IO;
using CapriKit.IO.Streams;
using System.Buffers;

namespace CapriKit.AssetPipeline.vNext;

// TODO: this and the promises in CapriKit.Concurrency implement the ideas from
// research\AssetPipelineLoadingGroups.md
// but there are still a few open questions
// - I need a blocking one so that all systems can start (Bootstrap), is that await? In CapriKit.Test.Tool I seem to be able to avoid that
// - I need a non-blocking one for all the other assets for stuff that should happen during loading screens what mechanism to use?
//   - LightWeightChannel to the rescue and then swapping the entire 'loading scene' with the new scene?
// - When does the actual loading start and how do we register for it? See AssetManager.Bundle
// - How/when do we set the owner of the promise for the extra check?

public abstract class AssetBundle
{
    private int outstanding;

    internal void OnRequestCompleted(LightweightChannel<AssetBundle> ready)
    {
        if (Interlocked.Decrement(ref outstanding) == 0)
        {
            ready.Write(this);
        }
    }

    internal abstract void Materialize();
}

public sealed class AssetBundle<T> : AssetBundle
{
    private readonly int Id;
    private readonly Func<PromiseResolver, T> Resolver;
    private readonly TaskCompletionSource<T> Source = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal AssetBundle(int id, Func<PromiseResolver, T> resolver)
    {
        Id = id;
        Resolver = resolver;
    }

    internal override void Materialize()
    {
        Source.SetResult(Resolver(new PromiseResolver(Id)));
    }

    public Task<T> Completion => Source.Task;
}


public sealed record ExampleAssets(object A, string B)
{
    public static AssetBundle<ExampleAssets> Define(AssetManager assetManager)
    {
        var transcoder = new ExampleTranscoder();

        var a = assetManager.Load(new AssetId("key", "path"), transcoder, default);
        var b = assetManager.Load(new AssetId("key", "path"), transcoder, default);

        return assetManager.Bundle(r => new ExampleAssets(r.Get(a), r.Get(b)));
    }

    public static async Task Foo(AssetBundle<ExampleAssets> bundle)
    {
        ExampleAssets assets = await bundle.Completion;
    }
}


public sealed class ExampleTranscoder() : NoSettingsTranscoder<string>(Guid.NewGuid(), 1)
{
    public override Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        writer.Write("Hello World");
        return Task.CompletedTask;
    }

    public override string Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        return reader.ReadString();
    }

    public override void HotSwap(string instance, string newParts)
    {
        throw new NotImplementedException();
    }
}
