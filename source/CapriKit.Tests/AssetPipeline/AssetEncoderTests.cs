using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Buffers;
using System.Buffers;

namespace CapriKit.Tests.AssetPipeline;

internal class AssetEncoderTests
{
    [Test]
    public async Task Encode()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "héllo");
        var transcoder = new DummyTranscoder();
        var id = new AssetId("Main", "hello.txt");

        await AssetEncoder.Encode(id, fileSystem, transcoder);
        var bytes = await fileSystem.ReadAllBytes("hello.txt.cka");

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
}
