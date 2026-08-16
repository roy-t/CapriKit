using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Streams;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal class HotReloadManagerV3Tests
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

        using var cache = new AssetCache();
        var live = cache.PutOrLease(id, asset.Value);

        using var sut = new HotReloadManagerV3(NullLoggerFactory.Instance, cache, fileSystem, NoDebounce);
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

        var transcoder = new FailingTranscoder();
        var id = new AssetId(AssetFile);

        await AssetEncoder.Encode(id, transcoder, default, fileSystem);
        var asset = await AssetDecoder.Decode(id, transcoder, fileSystem);

        using var cache = new AssetCache();
        var live = cache.PutOrLease(id, asset.Value);

        var sut = new HotReloadManagerV3(NullLoggerFactory.Instance, cache, fileSystem, NoDebounce);
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

    // Hot-swapping needs an asset that can be updated in place, so a mutable holder instead of a plain string
    private sealed class TextAsset(string text)
    {
        public string Text { get; set; } = text;
    }

    private class TextTranscoder() : NoSettingsTranscoder<TextAsset>(Guid.Parse("{6E4A1D0C-1F73-4C4E-9D2E-0B7F5C6A9E31}"), 1)
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

    private sealed class FailingTranscoder : TextTranscoder
    {
        public bool ShouldFail { get; set; }

        /// <summary>Written on a thread pool thread, only safe to read once the rebuild completed.</summary>
        public int FailedAttempts { get; private set; }

        public override Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
        {
            if (ShouldFail)
            {
                FailedAttempts++;
                throw new InvalidOperationException("Rebuilding this asset fails on purpose");
            }

            return base.Encode(id, fileSystem, writer);
        }
    }
}
