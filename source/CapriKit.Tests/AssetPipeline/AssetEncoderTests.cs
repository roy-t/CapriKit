using CapriKit.AssetPipeline;
using CapriKit.IO;
using CapriKit.IO.Streams;
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

        await AssetEncoder.Encode(id, new NoSettings<string>(), transcoder, fileSystem);
        var bytes = await fileSystem.ReadAllBytes("hello.txt.Main.cka");

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
        var encoderId = reader.ReadGuid();
        var encoderVersion = reader.ReadInt32();
        var settingsLength = reader.ReadInt32();
        reader.Advance(settingsLength);
        var payloadLength = reader.ReadInt32();
        reader.Advance(payloadLength);
        var dependencyCount = reader.ReadInt32();
        var lastWrite = reader.ReadInt64();
        var dependency = reader.ReadString();
        var end = reader.End;

        DateTime expectedTimeStamp = DateTime.Now;

        await Assert.That(encoderId).IsEqualTo(transcoder.Id);
        await Assert.That(encoderVersion).IsEqualTo(transcoder.Version);
        await Assert.That(settingsLength).IsEqualTo(0);
        await Assert.That(dependencyCount).IsEqualTo(1);
        await Assert.That(new DateTime(lastWrite))
            .IsBetween(expectedTimeStamp.AddMinutes(-1), expectedTimeStamp.AddMinutes(1));
        await Assert.That(dependency).IsEqualTo("hello.txt");
        await Assert.That(end).IsTrue();
    }
}
