using CapriKit.AssetPipeline;
using CapriKit.IO;

namespace CapriKit.Tests.AssetPipeline;

internal class AssetDecoderTests
{

    [Test]
    public async Task Decode()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "héllo");
        var transcoder = new DummyTranscoder();
        var id = new AssetId("Main", "hello.txt");

        await AssetEncoder.Encode(id, new NoSettings<string>(), transcoder, fileSystem);
        var job = await AssetDecoder.Decode(id, transcoder, fileSystem);

        FilePath expectedDependency = "hello.txt";
        DateTime expectedTimeStamp = DateTime.Now;

        var success = job.OnSuccess(out var asset);
        await Assert.That(success).IsTrue();

        await Assert.That(asset!.Value).IsEqualTo("HÉLLO");
        await Assert.That(asset.Dependencies.Count).IsEqualTo(1);
        await Assert.That(asset.Dependencies.First().File).IsEqualTo(expectedDependency);
        await Assert.That(asset.Dependencies.First().Version)
            .IsBetween(expectedTimeStamp.AddMinutes(-1), expectedTimeStamp.AddMinutes(1));
    }
}
