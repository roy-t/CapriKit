using System.Numerics;
using System.Runtime.CompilerServices;

namespace CapriKit.Geometry;

/// <summary>
/// Material properties.
/// </summary>
/// <param name="BaseColor">Base color in linear space.</param>
/// <param name="Metallic">Zero (0) for dielectric materials One (1) for conductors.</param>
/// <param name="Roughness">Roughness in [0..1].</param>
/// <param name="EmissionColor">Emission color in linear space.</param>
/// <param name="EmissionStrength">Strength of the emitted light in arbitrary units [0..inf).</param>
public readonly record struct CKTMaterial(Vector3 BaseColor, float Metallic, float Roughness, Vector3 EmissionColor, float EmissionStrength);

/// <summary>
/// An individual mesh in a model, representing a specific and unique LOD.
/// </summary>
/// <param name="VertexOffset">Offset to the first vertex of this mesh in the vertex array.</param>
/// <param name="VertexCount">Number of vertices in this mesh.</param>
/// <param name="IndexOffset">Offset to the first index of this mesh in the indices array.</param>
/// <param name="IndexCount">Number of indices in this mesh.</param>
/// <param name="TriangleOffset">Offset to the first triangle of this mesh in the triangles array</param>
/// <param name="TriangleCount">Number of triangles in this mesh</param>
/// <param name="BoundsMin"></param>
/// <param name="BoundsMax"></param>
public readonly record struct CKTMesh(uint VertexOffset, uint VertexCount, uint IndexOffset, uint IndexCount, uint TriangleOffset, uint TriangleCount, Vector3 BoundsMin, Vector3 BoundsMax);

/// <summary>
/// A vertex with a position and normal
/// </summary>
/// <param name="Position">Position in meters from an arbitrary origin</param>
/// <param name="Normal">Normalized normal for lighting calculations</param>
public readonly record struct CKTVertex(Vector3 Position, Vector3 Normal);

/// <summary>
/// A triangle with an assigned material.
/// </summary>
/// <param name="MaterialIndex">Index into the materials array</param>
public readonly record struct CKTTriangle(uint MaterialIndex);

/// <summary>
/// Should always spell "CapriKit.Textureless.Model" for valid files in ASCII bytes.
/// </summary>
[InlineArray(26)]
public struct FileTypeIdentifier
{
    private byte Elements;

    public static FileTypeIdentifier Create()
    {
        var id = new FileTypeIdentifier();
        Expected.CopyTo(id);
        return id;
    }

    private static ReadOnlySpan<byte> Expected => "CapriKit.Textureless.Model"u8;
    public readonly bool IsValid => Expected.SequenceEqual(this);
}

public readonly record struct CKTHeader(FileTypeIdentifier FileType, int FileTypeVersion, string Name, int MaterialCount, int MeshCount, int VertexCount, int IndexCount, int TriangleCount);

/// <summary>
/// Data for a CapriKit Textureless Model. A 3D model with 1..n LODs where each triangle is assigned a material instead of a texture.
/// Assumes:
/// - The coordinate system is right handed, (X+ is right, Y+ is up, Z+ is closer)
/// - Triangles are defined in clockwise order
/// - Units are in meters
/// </summary>
public sealed class CKTModelData
{
    public CKTModelData(CKTHeader header, CKTMesh[] meshes, CKTMaterial[] materials, CKTVertex[] vertices, uint[] indices, CKTTriangle[] triangles)
    {
        Header = header;
        Meshes = meshes;
        Materials = materials;
        Vertices = vertices;
        Indices = indices;
        Triangles = triangles;
    }

    public CKTHeader Header { get; }

    /// <summary>
    /// The materials used in the model. Materials are shared between meshes so when rendering a mesh you need
    /// access to the entire array.
    /// </summary>
    public CKTMaterial[] Materials { get; }

    /// <summary>
    /// The individual meshes in the model. Each mesh represent a different Level-Of-Detail (LOD).
    /// Meshes are ordered from highest to lowest detail.
    /// </summary>
    public CKTMesh[] Meshes { get; }

    /// <summary>    
    /// The vertices used by each mesh laid out in one larger array. Slices of this array give you
    /// the vertices needed to render individual meshes. Within such a slice all vertices are unique.
    /// </summary>
    public CKTVertex[] Vertices { get; }

    /// <summary>
    /// The indices used by each mesh laid out in one larger array. Slices of this array give you 
    /// the indices needed to render individual meshes. Indices are in the triangle list format.
    /// Index values are relative offsets into the slice of vertices that belong to the mesh,
    /// not absolute indexes into the entire vertices array.
    /// </summary>
    public uint[] Indices { get; }

    /// <summary>
    /// Extra per-triangle data for each mesh, laid out in one larger array. Slices of this array give you
    /// the data needed to render individual meshes. For every three indices there is exactly one entry in triangles.
    /// The first three vertices in the vertices slice of your LOD should use the triangle data from the first triangle
    /// struct in the slice for this LOD. Use SV_PrimitiveID in your shader and use a sliced SRV (or an offset).
    /// </summary>
    public CKTTriangle[] Triangles { get; }
}
