using CapriKit.Geometry;
using CapriKit.IO.Streams;

namespace CapriKit.Tests.Geometry;

internal class CKTModelParserTests
{
    [Test]
    public async Task Parse()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", CapriKit.AssetHandles.Models.Container1tue.ckt);
        var stream = File.OpenRead(path);

        var model = await CKTModelParser.Parse(stream, true);

        await Assert.That(model.Header.FileType.IsValid).IsTrue();
        await Assert.That(model.Header.FileTypeVersion).IsEqualTo(1);

        await Assert.That(model.Meshes.Length).IsEqualTo(1);
        var mesh = model.Meshes[0];

        await Assert.That(mesh.VertexOffset).IsEqualTo(0u);
        await Assert.That(mesh.VertexCount).IsEqualTo((uint)model.Header.VertexCount);

        await Assert.That(mesh.IndexOffset).IsEqualTo(0u);
        await Assert.That(mesh.IndexCount).IsEqualTo((uint)model.Header.IndexCount);

        await Assert.That(mesh.TriangleOffset).IsEqualTo(0u);
        await Assert.That(mesh.TriangleCount).IsEqualTo((uint)model.Header.TriangleCount);

        await Assert.That(model.Vertices.Count).IsEqualTo(model.Header.VertexCount);
        await Assert.That(model.Indices.Count).IsEqualTo(model.Header.IndexCount);
        await Assert.That(model.Triangles.Count).IsEqualTo(model.Header.TriangleCount);

        await Assert.That(stream.Position).IsEqualTo(stream.Length);
    }

    [Test]
    public async Task Parse_SequenceReader()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", CapriKit.AssetHandles.Models.Container1tue.ckt);
        var bytes = await File.ReadAllBytesAsync(path);
        var expected = await CKTModelParser.Parse(new MemoryStream(bytes));

        var (model, remaining) = ParseFromSequence(bytes);

        await Assert.That(model.Header.FileType.IsValid).IsTrue();
        await Assert.That(model.Header.Name).IsEqualTo(expected.Header.Name);
        await Assert.That(model.Materials).IsEquivalentTo(expected.Materials);
        await Assert.That(model.Meshes).IsEquivalentTo(expected.Meshes);
        await Assert.That(model.Vertices).IsEquivalentTo(expected.Vertices);
        await Assert.That(model.Indices).IsEquivalentTo(expected.Indices);
        await Assert.That(model.Triangles).IsEquivalentTo(expected.Triangles);
        await Assert.That(remaining).IsEqualTo(0L);
    }

    // SequenceReader cannot be passed by ref inside an async method
    private static (CKTModelData, long) ParseFromSequence(byte[] bytes)
    {
        var reader = SequenceReaders.Create(bytes, 0, bytes.Length);
        var model = CKTModelParser.Parse(ref reader, true);
        return (model, reader.Remaining);
    }
}
