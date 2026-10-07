using CapriKit.IO.Streams;
using System.Buffers;
using System.Text;
using static CapriKit.Geometry.CKTModelDataValidator;

namespace CapriKit.Geometry;

public static class CKTModelParser
{
    public static async Task<CKTModelData> Parse(Stream stream, bool validateData = false, CancellationToken cancellationToken = default)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var header = ReadHeader(reader);
        ValidateHeader(header);

        var materials = await stream.BlitArrayAsync<CKTMaterial>(header.MaterialCount, cancellationToken);
        if (validateData) { ValidateMaterials(header, materials); }

        var meshes = await stream.BlitArrayAsync<CKTMesh>(header.MeshCount, cancellationToken);
        if (validateData) { ValidateMeshes(header, meshes); }

        var vertices = await stream.BlitArrayAsync<CKTVertex>(header.VertexCount, cancellationToken);
        if (validateData) { ValidateVertices(header, meshes, vertices); }

        var indices = await stream.BlitArrayAsync<uint>(header.IndexCount, cancellationToken);
        if (validateData) { ValidateIndices(header, meshes, indices); }

        var triangles = await stream.BlitArrayAsync<CKTTriangle>(header.TriangleCount, cancellationToken);
        if (validateData) { ValidateTriangles(header, triangles); }

        return new CKTModelData(header, materials, meshes, vertices, indices, triangles);
    }

    private static CKTHeader ReadHeader(BinaryReader reader)
    {
        var id = ReadFileTypeIdentifier(reader);
        var version = reader.ReadInt32();
        var name = reader.ReadString();
        var materialCount = reader.ReadInt32();
        var meshCount = reader.ReadInt32();
        var vertexCount = reader.ReadInt32();
        var indexCount = reader.ReadInt32();
        var triangleCount = reader.ReadInt32();

        return new CKTHeader(id, version, name, materialCount, meshCount, vertexCount, indexCount, triangleCount);
    }

    private static FileTypeIdentifier ReadFileTypeIdentifier(BinaryReader reader)
    {
        var fileType = reader.ReadBytes(FileTypeIdentifier.Length);
        var id = new FileTypeIdentifier();
        fileType.CopyTo(id);
        return id;
    }
}
