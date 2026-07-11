using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal class AssetTranscoderTests
{

    [Test]
    public async Task Decode()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "héllo");
        var transcoder = new DummyTranscoder();
        var id = new AssetId("Main", "hello.txt");

        await AssetTranscoder.Encode(id, fileSystem, transcoder);
        var envelope = await AssetTranscoder.Decode(id, fileSystem, transcoder);

        FilePath expectedDependency = "hello.txt";
        await Assert.That(envelope.Asset).IsEqualTo("HÉLLO");
        await Assert.That(envelope.Dependencies.Count).IsEqualTo(1);
        await Assert.That(envelope.Dependencies.First()).IsEqualTo(expectedDependency);
    }

    [Test]
    public async Task Encode()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "héllo");
        var transcoder = new DummyTranscoder();
        var id = new AssetId("Main", "hello.txt");

        await AssetTranscoder.Encode(id, fileSystem, transcoder);
        var bytes = await fileSystem.ReadAllBytes("hello.txt.cka");

        // SequenceReader<byte> is a ref struct, so all reading happens before the first
        // await and only plain locals cross into the assertions
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
        var encoderId = reader.ReadGuid();
        var encoderVersion = reader.ReadInt32();
        var payloadLength = reader.ReadInt32();
        reader.Advance(payloadLength);
        var dependencyCount = reader.ReadInt32();
        var dependency = reader.ReadString();
        var end = reader.End;

        await Assert.That(encoderId).IsEqualTo(transcoder.Id);
        await Assert.That(encoderVersion).IsEqualTo(transcoder.Version);
        await Assert.That(dependencyCount).IsEqualTo(1);
        await Assert.That(dependency).IsEqualTo("hello.txt");
        await Assert.That(end).IsTrue();
    }

    /// <summary>
    /// Uppercases a text file
    /// </summary>
    private sealed class DummyTranscoder : IAssetEncoder, IAssetDecoder<string>
    {
        public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>([".txt"]);
        public Guid Id => Guid.Parse("{B87F41E3-6C33-46E4-802A-3E1E82800E7A}");
        public int Version => 1;

        public async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
        {
            var text = await fileSystem.ReadAllText(id.Path);
            writer.Write(text.ToUpperInvariant());
        }

        public string Decode(AssetId id, ref SequenceReader<byte> reader)
        {
            return reader.ReadString();
        }

        public void HotSwap(string instance, string replacement)
        {
            throw new NotImplementedException();
        }
    }
}
