using System.Numerics;

namespace CapriKit.Geometry;

public class ValidationException(string message) : Exception(message);

public static class CKTModelDataValidator
{
    /// <summary>
    /// Does a thorough, but expensive, sanity check on the CKTModelData to ensure all values are
    /// valid and all references point to elements in valid ranges.
    /// </summary>
    public static void Validate(CKTModelData data)
    {
        ValidateHeader(data.Header);
        ValidateMaterials(data.Header, data.Materials);
        ValidateMeshes(data.Header, data.Meshes);
        ValidateVertices(data.Header, data.Meshes, data.Vertices);
        ValidateIndices(data.Header, data.Meshes, data.Indices);
        ValidateTriangles(data.Header, data.Triangles);
    }

    /// <summary>
    /// Validates the header.
    /// </summary>
    internal static void ValidateHeader(CKTHeader header)
    {
        if (!header.FileType.IsValid) throw new ValidationException($"Unsupported file type: {header.FileType}");
        if (header.FileTypeVersion != 1) throw new ValidationException($"Unsupported file type version: {header.FileTypeVersion}");

        if (header.MaterialCount < 1)
        {
            throw new ValidationException($"Invalid material count: {header.MaterialCount}, a CKTModel should have at least one material");
        }

        if (header.MeshCount < 1)
        {
            throw new ValidationException($"Invalid mesh count: {header.MeshCount}, a CKTModel should have at least one mesh");
        }

        if (header.VertexCount < 3)
        {
            throw new ValidationException($"Invalid vertex count: {header.VertexCount}, a CKTModel should have at least three vertices (one triangle)");
        }

        if (header.IndexCount < 3)
        {
            throw new ValidationException($"Invalid index count: {header.IndexCount}, a CKTModel should have at least three indices (one triangle)");
        }

        if (header.TriangleCount < 1)
        {
            throw new ValidationException($"Invalid triangle count: {header.TriangleCount}, a CKTModel should have at least one triangle");
        }

        if (header.TriangleCount * 3L != header.IndexCount)
        {
            throw new ValidationException($"Invalid index count: {header.IndexCount}, which is NOT 3 times the number of triangles in the model ({header.TriangleCount})");
        }
    }

    /// <summary>
    /// Validates the materials array, assumes the other parameters have been validated.
    /// </summary>
    internal static void ValidateMaterials(CKTHeader header, CKTMaterial[] materials)
    {
        if (header.MaterialCount != materials.Length)
        {
            throw new ValidationException($"Length of materials array: {materials.Length} disagrees with length specified in header: {header.MaterialCount}");
        }

        for (var i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (!Vector3.AllWhereAllBitsSet(Vector3.IsFinite(material.BaseColor))
                || !Vector3.GreaterThanOrEqualAll(material.BaseColor, Vector3.Zero))
            {
                throw new ValidationException($"Material {i} has an invalid base color {material.BaseColor}");
            }

            if (!float.IsFinite(material.Metallic) || material.Metallic < 0 || material.Metallic > 1.0f)
            {
                throw new ValidationException($"Material {i} has an invalid metallic property {material.Metallic}");
            }

            if (!float.IsFinite(material.Roughness) || material.Roughness < 0 || material.Roughness > 1.0f)
            {
                throw new ValidationException($"Material {i} has an invalid roughness property {material.Roughness}");
            }

            if (!Vector3.AllWhereAllBitsSet(Vector3.IsFinite(material.EmissionColor))
                || !Vector3.GreaterThanOrEqualAll(material.EmissionColor, Vector3.Zero))
            {
                throw new ValidationException($"Material {i} has an invalid emission color {material.EmissionColor}");
            }

            if (!float.IsFinite(material.EmissionStrength) || material.EmissionStrength < 0)
            {
                throw new ValidationException($"Material {i} has an invalid emission strength property {material.EmissionStrength}");
            }
        }
    }

    /// <summary>
    /// Validates the meshes array, assumes the other parameters have been validated.
    /// </summary>
    internal static void ValidateMeshes(CKTHeader header, CKTMesh[] meshes)
    {
        if (header.MeshCount != meshes.Length)
        {
            throw new ValidationException($"Length of meshes array: {meshes.Length} disagrees with length specified in header: {header.MeshCount}");
        }

        for (var i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            if ((long)mesh.TriangleCount * 3L != mesh.IndexCount)
            {
                throw new ValidationException($"Mesh {i} has {mesh.IndexCount} indices, which is NOT 3 times the number of triangles in the mesh ({mesh.TriangleCount})");
            }
            if ((long)mesh.VertexOffset + mesh.VertexCount > header.VertexCount)
            {
                throw new ValidationException($"Mesh {i} uses slice: [{mesh.VertexOffset}..{mesh.VertexOffset + mesh.VertexCount}) of the vertices array which is out of range of the array ([0..{header.VertexCount})).");
            }
            if ((long)mesh.IndexOffset + mesh.IndexCount > header.IndexCount)
            {
                throw new ValidationException($"Mesh {i} uses slice: [{mesh.IndexOffset}..{mesh.IndexOffset + mesh.IndexCount}) of the indices array which is out of range of the array ([0..{header.IndexCount})).");
            }
            if ((long)mesh.IndexOffset != mesh.TriangleOffset * 3L)
            {
                throw new ValidationException($"Mesh {i} uses index offset: {mesh.IndexOffset} which is not 3 times triangle offset: {mesh.TriangleOffset}.");
            }
            if ((long)mesh.TriangleOffset + mesh.TriangleCount > header.TriangleCount)
            {
                throw new ValidationException($"Mesh {i} uses slice: [{mesh.TriangleOffset}..{mesh.TriangleOffset + mesh.TriangleCount}) of the triangle array which is out of range of the array ([0..{header.TriangleCount})).");
            }
            if (!Vector3.AllWhereAllBitsSet(Vector3.IsFinite(mesh.BoundsMin)) ||
                !Vector3.AllWhereAllBitsSet(Vector3.IsFinite(mesh.BoundsMax)) ||
                !Vector3.GreaterThanOrEqualAll(mesh.BoundsMax, mesh.BoundsMin))
            {
                throw new ValidationException($"Mesh {i} has invalid boundaries, min: {mesh.BoundsMin}, max: {mesh.BoundsMax}");
            }

            if (i > 0)
            {
                var previous = meshes[i - 1];
                if ((long)previous.VertexOffset + previous.VertexCount != mesh.VertexOffset)
                {
                    throw new ValidationException($"Noncontiguous use of vertices in slice [{(long)previous.VertexOffset + previous.VertexCount}..{mesh.VertexOffset})");
                }

                if ((long)previous.IndexOffset + previous.IndexCount != mesh.IndexOffset)
                {
                    throw new ValidationException($"Noncontiguous use of indices in slice [{(long)previous.IndexOffset + previous.IndexCount}..{mesh.IndexOffset})");
                }

                if ((long)previous.TriangleOffset + previous.TriangleCount != mesh.TriangleOffset)
                {
                    throw new ValidationException($"Noncontiguous use of triangles in slice [{(long)previous.TriangleOffset + previous.TriangleCount}..{mesh.TriangleOffset})");
                }
            }
        }
    }

    /// <summary>
    /// Validates the vertices array, assumes the other parameters have been validated.
    /// </summary>
    internal static void ValidateVertices(CKTHeader header, CKTMesh[] meshes, CKTVertex[] vertices)
    {
        const float NormalLengthTolerance = 1e-3f;

        if (header.VertexCount != vertices.Length)
        {
            throw new ValidationException($"Length of vertex array: {vertices.Length} disagrees with length specified in header: {header.VertexCount}");
        }

        for (var m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m];
            for (var i = mesh.VertexOffset; i < mesh.VertexOffset + mesh.VertexCount; i++)
            {
                var vertex = vertices[i];
                if (!Vector3.AllWhereAllBitsSet(Vector3.IsFinite(vertex.Position)))
                {
                    throw new ValidationException($"Vertex position {i} is invalid: {vertex.Position}");
                }

                var normal = vertex.Normal;
                if (!Vector3.AllWhereAllBitsSet(Vector3.IsFinite(normal)) ||
                    MathF.Abs(normal.LengthSquared() - 1.0f) > NormalLengthTolerance)
                {
                    throw new ValidationException($"Vertex normal {i} is invalid: {vertex.Normal}, length: {vertex.Normal.Length()}");
                }

                if (!Vector3.GreaterThanOrEqualAll(vertex.Position, mesh.BoundsMin) ||
                    !Vector3.GreaterThanOrEqualAll(mesh.BoundsMax, vertex.Position))
                {
                    throw new ValidationException($"Vertex position {i} with values {vertex.Position} is not contained by the boundaries of mesh {m}, min: {mesh.BoundsMin}, max: {mesh.BoundsMax}");
                }
            }
        }
    }


    /// <summary>
    /// Validates the indices array, assumes the other parameters have been validated.
    /// </summary>
    internal static void ValidateIndices(CKTHeader header, CKTMesh[] meshes, uint[] indices)
    {
        if (header.IndexCount != indices.Length)
        {
            throw new ValidationException($"Length of indices array: {indices.Length} disagrees with length specified in header: {header.IndexCount}");
        }

        for (var m = 0; m < meshes.Length; m++)
        {
            var mesh = meshes[m];
            for (var i = mesh.IndexOffset; i < mesh.IndexOffset + mesh.IndexCount; i++)
            {
                var index = indices[i];
                if (index >= mesh.VertexCount)
                {
                    throw new ValidationException($"Index {i} of mesh {m} references vertex with index {index} which is out of the range of the vertices available for this mesh: [0..{mesh.VertexCount}).");
                }
            }
        }
    }

    /// <summary>
    /// Validates the triangles array, assumes the other parameters have been validated.
    /// </summary>
    internal static void ValidateTriangles(CKTHeader header, CKTTriangle[] triangles)
    {
        if (header.TriangleCount != triangles.Length)
        {
            throw new ValidationException($"Length of triangles array: {triangles.Length} disagrees with length specified in header: {header.TriangleCount}");
        }

        for (var i = 0; i < triangles.Length; i++)
        {
            var triangle = triangles[i];
            if (triangle.MaterialIndex >= header.MaterialCount)
            {
                throw new ValidationException($"Triangle {i} references material index {triangle.MaterialIndex} which is outside of the range of Materials: [0..{header.MaterialCount}).");
            }
        }
    }
}
