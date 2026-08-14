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
        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);

        var transcoder = new TestTranscoder();
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(string.Empty, AssetFile);

        var builder = new AssetBundleBuilder(assetManager);
        var handle = builder.Load<string, NoSettings>(id, default);
        var loader = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? bundle = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out bundle);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(bundle).IsNotNull();
        await Assert.That(bundle.Text).IsEqualTo(TranscoderText);
    }

    private record TestBundle(string Text);

    private class TestTranscoder() : NoSettingsTranscoder<string>(Guid.Parse("{AC2D4E77-0D98-43B2-B1D2-35B0E9F5742B}"), 1)
    {
        public override string Decode(AssetId id, ref SequenceReader<byte> reader)
        {
            return reader.ReadString();
        }

        public override async Task Encode(AssetId id, IReadOnlyVirtualFileSystem fileSystem, IBufferWriter<byte> writer)
        {
            var text = await fileSystem.ReadAllText(id.Path);
            writer.Write(text);
        }

        public override void HotSwap(string instance, string newParts)
        {
            throw new NotImplementedException();
        }
    }
}
