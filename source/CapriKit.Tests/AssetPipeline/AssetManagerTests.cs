using CapriKit.AssetPipeline;
using CapriKit.Collections;
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

        var fileSystem = new FileSystem().ScopedTo(WorkingDirectory);
        await fileSystem.WriteAllText(AssetFile, "Hello World");

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);

        var transcoder = new TextTranscoder();
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset, NoSettings>(id, default);
        var bundle = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return bundle.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(contents).IsNotNull();
        await Assert.That(contents.Asset.Text).IsEqualTo(TranscoderText);

        // Load again to verify loading the same thing twice gives us the cached value. It comes straight
        // from the cache, so this bundle is ready the moment it is built, without a single Update.
        var altBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var altHandle = altBuilder.Request<TextAsset, NoSettings>(id, default);
        var altBundle = altBuilder.Build(resolver => new TestBundle(resolver.Get(altHandle)));

        await Assert.That(altBundle.IsReady(out var altContents)).IsTrue();
        await Assert.That(altContents!.Asset).IsSameReferenceAs(contents.Asset);

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
    /// The contents are built once and then handed out again on every later call. Rebuilding them would run
    /// the caller's factory once per frame, and forgetting to keep them hands back null through a
    /// contract that promises otherwise.
    /// </summary>
    [Test]
    public async Task IsReady_KeepsHandingOutTheSameContents()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(new TextTranscoder());

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));

        var built = 0;
        var bundle = builder.Build(resolver => { built++; return new TestBundle(resolver.Get(handle)); });

        TestBundle? first = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return bundle.IsReady(out first);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(bundle.IsReady(out var second)).IsTrue();
        await Assert.That(second).IsNotNull();
        await Assert.That(second).IsSameReferenceAs(first);
        await Assert.That(built).IsEqualTo(1);

        bundle.Dispose();
        assetManager.Update();
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Two bundles that ask for the same asset while it is still being built share one request, and both
    /// have to be handed the result. The waiters live in a <see cref="OneOrMany{T}"/> inside the manager's
    /// dictionary, so a second waiter is only really recorded if it is added through a reference to the
    /// stored value rather than to a copy of it.
    /// </summary>
    [Test]
    public async Task Load_HandsOneInFlightAssetToEveryBundleWaitingForIt()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TrackingTextTranscoder();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        // Both bundles are built before the first Update, so the second one joins the request that the
        // first one started instead of finding the asset in the cache
        var firstBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var firstHandle = firstBuilder.Request<TextAsset>(id);
        var first = firstBuilder.Build(resolver => new TestBundle(resolver.Get(firstHandle)));

        var secondBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var secondHandle = secondBuilder.Request<TextAsset>(id);
        var second = secondBuilder.Build(resolver => new TestBundle(resolver.Get(secondHandle)));

        TestBundle? firstContents = null;
        TestBundle? secondContents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return first.IsReady(out firstContents) && second.IsReady(out secondContents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // The asset was built once and shared, and each bundle took its own lease on it
        await Assert.That(transcoder.Decoded.Count).IsEqualTo(1);
        await Assert.That(secondContents!.Asset).IsSameReferenceAs(firstContents!.Asset);

        first.Dispose();
        assetManager.Update();
        await Assert.That(firstContents.Asset.IsDisposed).IsFalse();

        second.Dispose();
        assetManager.Update();
        await Assert.That(firstContents.Asset.DisposeCount).IsEqualTo(1);

        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Unloading a bundle whose assets are still on their way used to leak them: the bundle had taken no
    /// lease yet so Dispose had nothing to return, but the load still landed in a later Update and took one
    /// that nobody would ever give back. Update now returns the lease of an asset that its bundle refuses.
    /// </summary>
    [Test]
    public async Task Unload_ReturnsTheLeaseOfAnAssetThatWasStillLoading()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TrackingTextTranscoder();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));
        var bundle = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        // Act: unload before the first Update, so the asset is still loading and holds no lease yet
        bundle.Dispose();

        // The load still finishes and still takes its lease, the manager has to hand that one straight back
        await Assert.That(() =>
        {
            assetManager.Update();
            return transcoder.Decoded.Count == 1 && transcoder.Decoded.All(asset => asset.IsDisposed);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // The bundle no longer owns its assets, so it refuses to hand them out rather than serving
        // disposed ones
        await Assert.That(() => bundle.IsReady(out _)).Throws<ObjectDisposedException>();

        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// A bundle holds exactly one lease per asset, which is what lets unloading walk its assets rather than
    /// count how often each was asked for. Requesting the same asset twice would be a lease nobody gives
    /// back, so it is refused up front instead of silently deduplicated.
    /// </summary>
    [Test]
    public async Task Request_RefusesTheSameAssetTwice()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        using var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(new TextTranscoder());

        var id = new AssetId(AssetFile);
        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        _ = builder.Request<TextAsset>(id);

        await Assert.That(() => builder.Request<TextAsset>(id)).Throws<InvalidOperationException>();
    }

    /// <summary>
    /// A request that throws half-way leaves the bundle holding whatever the requests before it already
    /// took, and that bundle never reaches the caller, so nobody else could unload it. Build has to clean up
    /// after itself before the exception leaves.
    /// </summary>
    [Test]
    public async Task Build_UnloadsWhatItTookWhenALaterRequestThrows()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(new TrackingTextTranscoder());

        var id = new AssetId(AssetFile);

        // Arrange: get the asset into the cache, so that the doomed bundle below really does take a lease
        // on it while it is being built rather than only queueing a load
        var firstBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var firstHandle = firstBuilder.Request<TextAsset>(id);
        var first = firstBuilder.Build(resolver => new TestBundle(resolver.Get(firstHandle)));

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return first.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Act: the cached asset is taken during Build, then the second request fails because nothing knows
        // how to load an UnregisteredAsset
        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(id);
        _ = builder.Request<UnregisteredAsset>(new AssetId(HealthyFile));

        await Assert.That(() => builder.Build(resolver => new TestBundle(resolver.Get(handle)))).Throws<Exception>();

        // Assert: the lease the failed build took was handed back, so the last owner disposing really is
        // the last one and shutting down stays quiet
        first.Dispose();
        assetManager.Update();
        await Assert.That(contents!.Asset.IsDisposed).IsTrue();
        await Assert.That(contents.Asset.DisposeCount).IsEqualTo(1);

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

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));
        var bundle = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        await Assert.That(() =>
        {
            assetManager.Update();
            return bundle.IsReady(out _);
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
    /// forgets the failed request while it hands the failure out, so a later request starts over.
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
        var failedBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var failedHandle = failedBuilder.Request<TextAsset>(id);
        var failedBundle = failedBuilder.Build(resolver => new TestBundle(resolver.Get(failedHandle)));

        AssetLoadException? failure = null;
        await Assert.That(() =>
        {
            // Not guarded on purpose: Update throwing here fails this test, which is the point
            assetManager.Update();

            try { failedBundle.IsReady(out _); }
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

        var retryBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var retryHandle = retryBuilder.Request<TextAsset>(id);
        var retryBundle = retryBuilder.Build(resolver => new TestBundle(resolver.Get(retryHandle)));

        // Control: a second, untouched asset requested at the same moment, to show that a failure never
        // stopped the manager as a whole and that the retry above is what actually changed.
        await fileSystem.WriteAllText(HealthyFile, TranscoderText);

        var healthyBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var healthyHandle = healthyBuilder.Request<TextAsset>(new AssetId(HealthyFile));
        var healthyBundle = healthyBuilder.Build(resolver => new TestBundle(resolver.Get(healthyHandle)));

        await Assert.That(() =>
        {
            assetManager.Update();
            return healthyBundle.IsReady(out _);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Assert: the retry really rebuilt the asset, so the failure, the control and the retry each
        // reached the transcoder exactly once
        TestBundle? retried = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return retryBundle.IsReady(out retried);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(transcoder.Attempts).IsEqualTo(3);
        await Assert.That(retried!.Asset.Text).IsEqualTo(TranscoderText);

        // The first bundle keeps reporting its failure rather than quietly never completing
        await Assert.That(() => failedBundle.IsReady(out _)).Throws<AssetLoadException>();

        // Every bundle can be unloaded, the failed one included: it returns nothing because its only asset
        // never took a lease. Getting that wrong would return the lease that retryBundle holds on the same
        // asset, so the clean dispose below is what proves the counting is right.
        failedBundle.Dispose();
        retryBundle.Dispose();
        healthyBundle.Dispose();
        assetManager.Update();
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Forgetting to register a transcoder is a programmer error rather than a broken asset, so it has to
    /// surface on the calling thread instead of arriving as a failed load a few frames later. Looking the
    /// transcoder up before the cache is checked keeps that true whether or not the asset happens to be
    /// cached already.
    /// </summary>
    [Test]
    public async Task Build_ThrowsOnTheCallingThreadWhenNoTranscoderIsRegistered()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        using var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));

        await Assert.That(() => builder.Build(resolver => new TestBundle(resolver.Get(handle)))).Throws<Exception>();
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

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));
        var bundle = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return bundle.IsReady(out contents);
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

        bundle.Dispose();
        assetManager.Update();
        assetManager.Dispose();
    }

    /// <summary>
    /// A load that failed never put anything in the pool, so there is no lease to hand back when the bundle
    /// that was waiting for it turns the failure down. Returning one anyway asks the pool for an entry that
    /// was never there, which it refuses, and that refusal escapes from <see cref="AssetManager.Update"/>
    /// on the primary thread.
    /// </summary>
    [Test]
    public async Task Update_ReturnsNoLeaseForAFailedLoadThatItsBundleRefuses()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TranscoderThatCanFail { ShouldFail = true };
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var builder = new AssetBundleBuilder<TestBundle>(assetManager);
        var handle = builder.Request<TextAsset>(new AssetId(AssetFile));
        var bundle = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        // Act: unload before the first Update, so the failure arrives at a bundle that refuses it
        bundle.Dispose();

        Exception? thrown = null;
        await Assert.That(() =>
        {
            try { assetManager.Update(); }
            catch (Exception ex) { thrown = ex; }
            return transcoder.FailedAttempts > 0 || thrown is not null;
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Pump once more so the failure that the build queued is actually delivered
        try { assetManager.Update(); }
        catch (Exception ex) { thrown ??= ex; }

        await Assert.That(thrown).IsNull();
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// Two bundles that joined the same request are handed the asset one after the other, and each hand-out
    /// takes its own lease. The pool evicts at zero, so returning the lease of a bundle that refuses before
    /// the next one has taken its own drops the asset to zero in between: it is queued for disposal, and
    /// the next hand-out puts the very same instance back in the pool as if it were new.
    /// </summary>
    [Test]
    public async Task Update_ReturnsOneLeasePerBundleWhenEveryWaitingBundleRefuses()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TrackingTextTranscoder();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        // Both bundles are built before the first Update, so the second joins the first one's request
        var firstBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var firstHandle = firstBuilder.Request<TextAsset>(id);
        var first = firstBuilder.Build(resolver => new TestBundle(resolver.Get(firstHandle)));

        var secondBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var secondHandle = secondBuilder.Request<TextAsset>(id);
        var second = secondBuilder.Build(resolver => new TestBundle(resolver.Get(secondHandle)));

        // Act: both are gone by the time the asset arrives, so both refuse it
        first.Dispose();
        second.Dispose();

        await Assert.That(() =>
        {
            assetManager.Update();
            return transcoder.Decoded.Count == 1 && transcoder.Decoded.All(asset => asset.IsDisposed);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Assert: the asset went through the pool once, not once per bundle that refused it
        await Assert.That(transcoder.Decoded.Single().DisposeCount).IsEqualTo(1);
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }

    /// <summary>
    /// The counterpart of <see cref="Update_ReturnsOneLeasePerBundleWhenEveryWaitingBundleRefuses"/>: the
    /// bundle that refuses is handed the asset first and the one that keeps it second. Dropping the asset
    /// to zero in between hands the surviving bundle an instance that is already queued for disposal, so it
    /// is disposed at the end of the very same Update that delivered it.
    /// </summary>
    [Test]
    public async Task Update_KeepsAnAssetAliveWhenOnlyTheFirstWaitingBundleRefuses()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, TranscoderText);

        var transcoder = new TrackingTextTranscoder();
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        var firstBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var firstHandle = firstBuilder.Request<TextAsset>(id);
        var first = firstBuilder.Build(resolver => new TestBundle(resolver.Get(firstHandle)));

        var secondBuilder = new AssetBundleBuilder<TestBundle>(assetManager);
        var secondHandle = secondBuilder.Request<TextAsset>(id);
        var second = secondBuilder.Build(resolver => new TestBundle(resolver.Get(secondHandle)));

        // Act: only the bundle that is handed the asset first goes away
        first.Dispose();

        TestBundle? contents = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return second.IsReady(out contents);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        // Assert: the bundle that is still alive owns a usable asset, not one the pool already let go of
        await Assert.That(contents!.Asset.IsDisposed).IsFalse();
        await Assert.That(contents.Asset.Text).IsEqualTo(TranscoderText);

        second.Dispose();
        assetManager.Update();
        await Assert.That(contents.Asset.DisposeCount).IsEqualTo(1);
        await Assert.That(() => assetManager.Dispose()).ThrowsNothing();
    }
}

internal record TestBundle(TextAsset Asset);

/// <summary>An asset type that deliberately has no transcoder, so that requesting it fails.</summary>
internal sealed class UnregisteredAsset;

internal sealed class TextAsset(string text) : IDisposable
{
    public string Text { get; set; } = text;

    /// <summary>Lets tests see whether the pool really let go of this asset.</summary>
    public bool IsDisposed => DisposeCount > 0;

    /// <summary>
    /// Counted rather than flagged: an asset that is disposed twice would double free the native
    /// resources of a real asset, so a test has to be able to tell one from two.
    /// </summary>
    public int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
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
