using CapriKit.AssetPipeline;
using CapriKit.IO;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal class HotReloadManagerTests
{
    private static readonly FilePath AssetFile = new("Hello.txt");
    private static readonly TimeSpan NoDebounce = TimeSpan.Zero;

    [Test]
    public async Task Update()
    {
        // Arrange: build, load and cache an asset the way the asset manager would
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, "Hello World");

        var transcoder = new TextTranscoder();
        var id = new AssetId(AssetFile);

        await AssetEncoder.Encode(id, transcoder, default, fileSystem);
        var asset = await AssetDecoder.Decode(id, transcoder, fileSystem);

        using var cache = new AssetPool();
        var live = cache.PutOrLease(id, asset.Value);

        using var sut = new HotReloadManager(NullLoggerFactory.Instance, cache, fileSystem, NoDebounce);
        sut.Track(asset, transcoder);

        // Act: change the file the asset was built from
        await fileSystem.WriteAllText(AssetFile, "Goodbye World");

        await Assert.That(() =>
        {
            sut.Update();
            return live.Text;
        })
        .Eventually(v => v.IsEqualTo("Goodbye World"), TimeSpan.FromSeconds(5));

        // Assert: the caller's instance was updated in place, and we left no lease behind
        await Assert.That(live.Text).IsEqualTo("Goodbye World");
        cache.Return(id);
    }

    [Test]
    public async Task Update_RebuildFails()
    {
        // Arrange: build, load and cache an asset, then make every following rebuild fail
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        await fileSystem.WriteAllText(AssetFile, "Hello World");

        var transcoder = new TranscoderThatCanFail();
        var id = new AssetId(AssetFile);

        await AssetEncoder.Encode(id, transcoder, default, fileSystem);
        var asset = await AssetDecoder.Decode(id, transcoder, fileSystem);

        using var cache = new AssetPool();
        var live = cache.PutOrLease(id, asset.Value);

        var sut = new HotReloadManager(NullLoggerFactory.Instance, cache, fileSystem, NoDebounce);
        sut.Track(asset, transcoder);
        transcoder.ShouldFail = true;

        // Act: one update starts the rebuild, disposing waits for it and finishes it
        await fileSystem.WriteAllText(AssetFile, "Goodbye World");
        sut.Update();
        sut.Dispose();

        // Assert: the rebuild really was attempted, the live asset kept its contents, and returning the last
        // lease empties the cache. If the manager leaked its lease the cache throws when it is disposed.
        await Assert.That(transcoder.FailedAttempts).IsEqualTo(1);
        await Assert.That(live.Text).IsEqualTo("Hello World");
        cache.Return(id);
    }
}



/// <summary>
/// Builds like a <see cref="TextTranscoder"/> until <see cref="ShouldFail"/> is set. Encoding runs on the
/// thread pool while tests flip the switch and read the counters from the main thread, so all three cross
/// that boundary explicitly.
/// </summary>
internal sealed class TranscoderThatCanFail : TextTranscoder
{
    private volatile bool shouldFail;
    private int attempts;
    private int failedAttempts;

    public bool ShouldFail { get => shouldFail; set => shouldFail = value; }

    /// <summary>Every build the manager asked for, whether it succeeded or not.</summary>
    public int Attempts => Volatile.Read(ref attempts);

    public int FailedAttempts => Volatile.Read(ref failedAttempts);

    public override Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
    {
        Interlocked.Increment(ref attempts);

        if (ShouldFail)
        {
            Interlocked.Increment(ref failedAttempts);
            throw new InvalidOperationException("Rebuilding this asset failed on purpose");
        }
        return base.Encode(id, fileSystem, writer);
    }
}
