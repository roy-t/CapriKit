using CapriKit.Geometry;

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
}
