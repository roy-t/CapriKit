using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Streams;
using CapriKit.Tests.TestUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.Collections.Concurrent;

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

        var bundle = assetManager.CreateBundle();
        var handle = bundle.Load<TextAsset, NoSettings>(id, default);
        var loader = bundle.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(contents).IsNotNull();
        await Assert.That(contents.Asset.Text).IsEqualTo(TranscoderText);

        // Load again to verify loading the same thing twice gives us the cached value
        var altBundle = assetManager.CreateBundle();
        var altHandle = altBundle.Load<TextAsset, NoSettings>(id, default);
        var altLoader = altBundle.Build(resolver => new TestBundle(resolver.Get(altHandle)));

        TestBundle? altContents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return altLoader.IsReady(out altContents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(altContents).IsNotNull();
        await Assert.That(altContents.Asset).IsSameReferenceAs(contents.Asset);

        // Both bundles lease the one shared instance, so it only really goes away once both gave it back
        bundle.Dispose();
        assetManager.Update();
        await Assert.That(contents.Asset.IsDisposed).IsFalse();

        altBundle.Dispose();
        assetManager.Update();
        await Assert.That(contents.Asset.IsDisposed).IsTrue();

        // Nothing is left over, so shutting down is quiet
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Unloading a bundle whose assets are still on their way used to leak them: the handles had taken no
    /// lease yet so Unload had nothing to return, but the load still landed in a later Update and took one
    /// that nobody would ever give back. Update now returns the lease of a handle whose bundle is gone.
    /// </summary>
    [Test]
    public async Task Unload_ReturnsTheLeaseOfAnAssetThatWasStillLoading()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TrackingTextTranscoder();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var bundle = assetManager.CreateBundle();
        var handle = bundle.Load<TextAsset>(new AssetId(AssetFile));
        var loader = bundle.Build(resolver => new TestBundle(resolver.Get(handle)));

        // Act: unload before the first Update, so the asset is still loading and holds no lease yet.
        // Disposing the bundle is the same thing as unloading it, and is how this is meant to be written.
        bundle.Dispose();

        // The load still finishes and still takes its lease, the manager has to hand that one straight back
        await Assert.That(() =>
        {
            assetManager.Update();
            return transcoder.Decoded.Count == 1 && transcoder.Decoded.All(asset => asset.IsDisposed);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // The bundle no longer owns its assets, so its loader refuses to hand them out rather than
        // serving disposed ones
        await Assert.That(() => loader.IsReady(out _)).Throws<ObjectDisposedException>();

        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Everything that is loaded has to be unloaded before the game quits. The pool notices when that did
    /// not happen but can only count the assets left behind, so the manager names the bundle they belong to
    /// and the line that created it. It deliberately does not unload them: at shutdown that would only hide
    /// the bug from whoever has to fix it.
    /// </summary>
    [Test]
    public async Task Dispose_ReportsBundlesThatWereNeverUnloaded()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var logger = new CapturingLoggerFactory();
        var assetManager = new AssetManager(logger, fileSystem);
        assetManager.RegisterTranscoder(new TrackingTextTranscoder());

        var bundle = assetManager.CreateBundle();
        var handle = bundle.Load<TextAsset>(new AssetId(AssetFile));
        var loader = bundle.Build(resolver => new TestBundle(resolver.Get(handle)));

        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out _);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Act: quit without unloading anything
        await Assert.That(() => assetManager.Dispose()).Throws<Exception>();

        var report = logger.Messages.SingleOrDefault(message => message.Contains("was never unloaded"));
        await Assert.That(report).IsNotNull();
        await Assert.That(report!.Contains($"{nameof(AssetManagerTests)}.cs:")).IsTrue();
    }

    /// <summary>
    /// A failed load used to wedge its asset id for the rest of the manager's life: the failure never
    /// cleared the entry in Outstanding, and because Load hands every later request for that id to the
    /// existing list instead of starting a new one, the asset could never be loaded again. Update now
    /// forgets the failed request before it rethrows, so a later Load starts over.
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

        // Act: the first load fails while building. Update stays quiet about it, the failure is handed to
        // the bundle that was waiting and surfaces from IsReady.
        var failedBundle = assetManager.CreateBundle();
        var failedHandle = failedBundle.Load<TextAsset>(id);
        var failedLoader = failedBundle.Build(resolver => new TestBundle(resolver.Get(failedHandle)));

        AssetLoadException? failure = null;
        await Assert.That(() =>
        {
            // Not guarded on purpose: Update throwing here fails this test, which is the point
            assetManager.Update();

            try { failedLoader.IsReady(out _); }
            catch (AssetLoadException ex) { failure = ex; }
            return failure is not null;
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // The exception names the asset that failed and keeps the transcoder's own exception as the cause
        await Assert.That(failure!.Asset).IsEqualTo(id);
        await Assert.That(failure!.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(transcoder.Attempts).IsEqualTo(1);

        // Act: take away the reason the build failed and ask for the very same asset again
        transcoder.ShouldFail = false;

        var retryBundle = assetManager.CreateBundle();
        var retryHandle = retryBundle.Load<TextAsset>(id);
        var retryLoader = retryBundle.Build(resolver => new TestBundle(resolver.Get(retryHandle)));

        // Control: a second, untouched asset requested at the same moment, to show that a failure never
        // stopped the manager as a whole and that the retry above is what actually changed.
        await fileSystem.WriteAllText(HealthyFile, TranscoderText);

        var healthyBundle = assetManager.CreateBundle();
        var healthyHandle = healthyBundle.Load<TextAsset>(new AssetId(HealthyFile));
        var healthyLoader = healthyBundle.Build(resolver => new TestBundle(resolver.Get(healthyHandle)));

        await Assert.That(() =>
        {
            assetManager.Update();
            return healthyLoader.IsReady(out _);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Assert: the retry really rebuilt the asset, so the failure, the control and the retry each
        // reached the transcoder exactly once
        TestBundle? retried = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return retryLoader.IsReady(out retried);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(transcoder.Attempts).IsEqualTo(3);
        await Assert.That(retried!.Asset.Text).IsEqualTo(TranscoderText);

        // The first bundle keeps reporting its failure rather than quietly never completing
        await Assert.That(() => failedLoader.IsReady(out _)).Throws<AssetLoadException>();

        // Every bundle can be unloaded, the failed one included: it returns nothing because its only handle
        // never took a lease. Getting that wrong would return the lease that retryBundle holds on the same
        // asset, so the clean dispose below is what proves the counting is right.
        assetManager.Unload(failedBundle);
        assetManager.Unload(retryBundle);
        assetManager.Unload(healthyBundle);
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Forgetting to register a transcoder is a programmer error rather than a broken asset, so it has to
    /// surface on the calling thread instead of arriving as a failed load a few frames later. Looking the
    /// transcoder up before the cache is checked keeps that true whether or not the asset happens to be
    /// cached already.
    /// </summary>
    [Test]
    public async Task Load_ThrowsOnTheCallingThreadWhenNoTranscoderIsRegistered()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        using var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        var bundle = assetManager.CreateBundle();

        await Assert.That(() => bundle.Load<TextAsset>(new AssetId(AssetFile))).Throws<Exception>();
    }

    /// <summary>
    /// Leases are counted per handle, not per distinct asset, so a bundle that asks for the same asset
    /// twice holds two leases and has to give back two. Unloading used to work from the distinct asset ids
    /// and gave back one, which leaked the asset for the rest of the program.
    /// </summary>
    [Test]
    public async Task Unload_ReturnsOneLeasePerHandle()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(new TextTranscoder());

        var id = new AssetId(AssetFile);

        var bundle = assetManager.CreateBundle();
        var first = bundle.Load<TextAsset>(id);
        var second = bundle.Load<TextAsset>(id);
        var loader = bundle.Build(resolver => new TwiceBundle(resolver.Get(first), resolver.Get(second)));

        TwiceBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Both handles resolved to the single cached instance, but each of them took its own lease
        await Assert.That(contents!.Second).IsSameReferenceAs(contents.First);

        assetManager.Unload(bundle);
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// The counterpart of <see cref="Load_RetriesAnAssetWhoseFirstLoadFailed"/>. A transcoder that throws
    /// while loading is fatal, but the very same transcoder throwing during a hot-reload must not be: the
    /// asset that is already live is still perfectly usable, and taking the game down over a development
    /// feature would be worse than the stale contents.
    /// </summary>
    [Test]
    public async Task Update_DoesNotThrowWhenAHotReloadFails()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TranscoderThatCanFail();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var bundle = assetManager.CreateBundle();
        var handle = bundle.Load<TextAsset>(new AssetId(AssetFile));
        var loader = bundle.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Act: make every rebuild fail, then change the file the asset was built from
        transcoder.ShouldFail = true;
        await fileSystem.WriteAllText(AssetFile, "Goodbye World");

        // Pump past the hot-reload debounce until the rebuild has actually been attempted and failed
        Exception? thrown = null;
        await Assert.That(() =>
        {
            try { assetManager.Update(); }
            catch (Exception ex) { thrown = ex; }
            return transcoder.FailedAttempts;
        })
        .Eventually(v => v.IsGreaterThan(0), TimeSpan.FromSeconds(10));

        // Assert: the failure stayed inside the hot-reload path and the asset kept its old contents
        await Assert.That(thrown).IsNull();
        await Assert.That(contents!.Asset.Text).IsEqualTo(TranscoderText);

        assetManager.Unload(bundle);
        assetManager.Dispose();
    }
}

internal record TestBundle(TextAsset Asset);

internal record TwiceBundle(TextAsset First, TextAsset Second);

internal sealed class TextAsset(string text) : IDisposable
{
    public string Text { get; set; } = text;

    /// <summary>Lets tests see whether the pool really let go of this asset.</summary>
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

/// <summary>
/// Hands out the assets it decoded so that a test can look at an asset the asset manager never gave it.
/// </summary>
internal sealed class TrackingTextTranscoder : TextTranscoder
{
    private readonly ConcurrentBag<TextAsset> decoded = [];

    public IReadOnlyCollection<TextAsset> Decoded => decoded;

    public override TextAsset Decode(AssetId id, ref SequenceReader<byte> reader)
    {
        var asset = base.Decode(id, ref reader);
        decoded.Add(asset);
        return asset;
    }
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
