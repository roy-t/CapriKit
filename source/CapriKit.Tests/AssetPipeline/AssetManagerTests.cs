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
        await fileSystem.WriteAllText(AssetFile, "Hello World");

        var assetManager = new AssetManager(NullLoggerFactory.Instance, fileSystem);

        var transcoder = new TextTranscoder();
        assetManager.RegisterTranscoder(transcoder);

        var id = new AssetId(AssetFile);

        var builder = assetManager.CreateBundle();
        var handle = builder.Load<TextAsset, NoSettings>(id, default);
        var loader = builder.Build(resolver => new TestBundle(resolver.Get(handle)));

        TestBundle? bundle = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return loader.IsReady(out bundle);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(bundle).IsNotNull();
        await Assert.That(bundle.Asset.Text).IsEqualTo(TranscoderText);

        // Load again to verify loading the same thing twice gives us the cached value
        var altBuilder = new AssetBundleBuilder(assetManager);
        var altHandle = altBuilder.Load<TextAsset, NoSettings>(id, default);
        var altLoader = altBuilder.Build(resolver => new TestBundle(resolver.Get(altHandle)));

        TestBundle? altBundle = null;
        await Assert.That(() =>
        {
            assetManager.Update();
            return altLoader.IsReady(out altBundle);
        })
        .Eventually(v => v.IsTrue(), TimeSpan.FromSeconds(5));

        await Assert.That(altBundle).IsNotNull();
        await Assert.That(altBundle.Asset).IsSameReferenceAs(bundle.Asset);

        // TODO: test the dispose path. Check the errors if assets are not returned and that returning both bundles correctly disposes everything.
    }
}

internal record TestBundle(TextAsset Asset);

internal sealed class TextAsset(string text)
{
    public string Text { get; set; } = text;
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
