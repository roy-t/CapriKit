using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Streams;
using CapriKit.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal class AssetManagerTests
{
    private DirectoryPath? WorkingDirectory;
    private readonly FilePath AssetFile = new("Hello.txt");
    private readonly FilePath HealthyFile = new("Goodbye.txt");
    private const string TranscoderText = "Hello World";

    [Before(Test)]
    public void Setup()
    {
        WorkingDirectory = FileSystemUtilities.CreateTemporaryDirectory();
        var fileSystem = new FileSystem().ScopedTo(WorkingDirectory);
        using var stream = fileSystem.CreateReadWrite(AssetFile);
        using var writer = new StreamWriter(stream);
        writer.Write(TranscoderText);
    }

    [After(Test)]
    public void TearDown()
    {
        Directory.Delete(WorkingDirectory, true);
    }

    [Test]
    public async Task LoadAsset()
    {
        await Assert.That(WorkingDirectory).IsNotNull();

        var logger = NullLoggerFactory.Instance;
        var fileSystem = new FileSystem().ScopedTo(WorkingDirectory);
        await fileSystem.WriteAllText(AssetFile, "Hello World");

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);

        var transcoder = new TextTranscoder();
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        var builder = assetManager.CreateBundle();
        var handle = builder.Load<TextAsset, NoSettings>(id, default);
        var loader = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? bundle = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out bundle);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(bundle).IsNotNull();
        await Assert.That(bundle.Asset.Text).IsEqualTo(TranscoderText);

        // Load again to verify loading the same thing twice gives us the cached value
        var altBuilder = new AssetBundleBuilder(assetManager);
        var altHandle = altBuilder.Load<TextAsset, NoSettings>(id, default);
        var altLoader = altBuilder.Build(resolver => new TestBundle(resolver.Get(altHandle)));

        TestBundle? altBundle = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return altLoader.IsReady(out altBundle);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(altBundle).IsNotNull();
        await Assert.That(altBundle.Asset).IsSameReferenceAs(bundle.Asset);

        // TODO: test the dispose path. Check the errors if assets are not returned and that returning both bundles correctly disposes everything.
    }

    /// <summary>
    /// FAILS ON PURPOSE: it asserts the behaviour we want, which the AssetManager does not have yet.
    ///
    /// A load that fails wedges its asset id for the rest of the manager's life. RequestAsset reports the
    /// failure through the incoming channel, but only its success path writes a materializer, so Update
    /// never reaches the `Outstanding.Remove(id)` that clears the request. The dead handle list survives,
    /// and because Load hands every later request for that id to the existing list instead of starting a
    /// new one, the asset can never be loaded again, not even after the cause of the failure is gone.
    ///
    /// The fix is to clear the Outstanding entry when a request fails, next to the `Incoming.Write(ex)`
    /// in AssetManager.Load, so that the next Load starts a fresh request.
    /// </summary>
    [Test]
    public async Task Load_RetriesAnAssetWhoseFirstLoadFailed()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TranscoderThatCanFail { ShouldFail = true };

        // Deliberately not a `using`: AssetPool.Dispose throws when leases are outstanding, and an exception
        // from a dispose during unwinding replaces the assertion that actually failed. Disposing at the end
        // keeps the leak check but lets a real failure report itself.
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        // Act: the first load fails while building and Update rethrows that failure on the main thread
        var failedBuilder = assetManager.CreateBundle();
        var failedHandle = failedBuilder.Load<TextAsset>(id);
        var failedLoader = failedBuilder.Build(resolver => new TestBundle(resolver.Get(failedHandle)));

        Exception? failure = null;
        await Assert.That(() =>
        {
            try { assetManager.Update(); }
            catch (Exception ex) { failure = ex; }
            return failure is not null;
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(failure).IsTypeOf<InvalidOperationException>();
        await Assert.That(transcoder.Attempts).IsEqualTo(1);

        // Act: take away the reason the build failed and ask for the very same asset again
        transcoder.ShouldFail = false;

        var retryBuilder = assetManager.CreateBundle();
        var retryHandle = retryBuilder.Load<TextAsset>(id);
        var retryLoader = retryBuilder.Build(resolver => new TestBundle(resolver.Get(retryHandle)));

        // Control: a second, untouched asset requested at the same moment. Waiting for it proves that a
        // failure does not stop the manager as a whole, so a red test really is about this one asset id.
        await fileSystem.WriteAllText(HealthyFile, TranscoderText);

        var healthyBuilder = assetManager.CreateBundle();
        var healthyHandle = healthyBuilder.Load<TextAsset>(new AssetId(HealthyFile));
        var healthyLoader = healthyBuilder.Build(resolver => new TestBundle(resolver.Get(healthyHandle)));

        await Assert.That(() =>
        {
            assetManager.Update();
            return healthyLoader.IsReady(out _);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Pump until the retry has built, and stop early once it has so that the green path stays quick.
        // Today it never happens and we spend the whole window, which is what makes the failure below crisp
        // instead of a five second timeout somewhere else.
        for (var i = 0; i < 200 && transcoder.Attempts < 3; i++)
        {
            assetManager.Update();
            await Task.Delay(10);
        }

        // Assert: the failure, the control and the retry each reached the transcoder exactly once.
        // THIS IS THE ASSERTION THAT FAILS TODAY: the retry never starts, so it stops at 2.
        await Assert.That(transcoder.Attempts).IsEqualTo(3);

        await Assert.That(() =>
        {
            assetManager.Update();
            return retryLoader.IsReady(out _);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // The first bundle stays unresolved even after the fix. Its exception already surfaced out of
        // Update and a handle carries no way to report one, so those handles are simply dead.
        await Assert.That(failedLoader.IsReady(out _)).IsFalse();

        // Only the two resolved bundles hold a lease, so a clean dispose also proves that the failed
        // request never leaked one of its own.
        assetManager.Unload(retryLoader);
        assetManager.Unload(healthyLoader);
        assetManager.Dispose();
    }
}

internal record TestBundle(TextAsset Asset);

internal sealed class TextAsset(string text)
{
    public string Text { get; set; } = text;
}

internal class TextTranscoder() : NoSettingsTranscoder<TextAsset>(Guid.Parse("{6E4A1D0C-1F73-4C4E-9D2E-0B7F5C6A9E31}"), 1)
{
    public override TextAsset Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        return new TextAsset(reader.ReadString());
    }

    public override async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        var text = await fileSystem.ReadAllText(id.Path);
        writer.Write(text);
    }

    public override void HotSwap(TextAsset instance, TextAsset newParts)
    {
        instance.Text = newParts.Text;
    }
}
