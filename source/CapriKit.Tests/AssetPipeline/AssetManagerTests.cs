using CapriKit.AssetPipeline;
using CapriKit.IO;

namespace CapriKit.Tests.AssetPipeline;

internal class AssetManagerTests
{
    [Test]
    public async Task Decode()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "héllo");
        var manager = new AssetManager(fileSystem);
        manager.RegisterTranscoder(new DummyTranscoder());
        var id = new AssetId("Main", "hello.txt");

        await manager.Encode<string>(id);
        var text = await manager.Decode<string>(id);

        await Assert.That(text).IsEqualTo("HÉLLO");
    }

    [Test]
    public async Task Decode_SettingsAreReadFromTheEncodedFile()
    {
        var fileSystem = new InMemoryFileSystem();
        await fileSystem.WriteAllText("hello.txt", "hey");
        var manager = new AssetManager(fileSystem);
        manager.RegisterTranscoder(new RepeatTranscoder());
        var id = new AssetId("Main", "hello.txt");

        // The asset type is inferred from the settings, decoding requires no settings at all
        await manager.Encode(id, new RepeatSettings(3));
        var text = await manager.Decode<string>(id);

        await Assert.That(text).IsEqualTo("heyheyhey");
    }
}
