using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Mathematics;

namespace CapriKit.AssetPipeline.DirectX11.Models;

/// <summary>
/// Material properties.
/// </summary>
/// <param name="BaseColor">Base color in linear space.</param>
/// <param name="Metallic">Zero (0) for dielectric materials One (1) for conductors.</param>
/// <param name="Roughness">Roughness in [0..1].</param>
/// <param name="EmissionColor">Emission color in linear space.</param>
/// <param name="EmissionStrength">Strength of the emitted light in arbitrary units [0..inf).</param>
public readonly record struct CKTMaterial(Color3 BaseColor, float Metallic, float Roughness, Color3 EmissionColor, float EmissionStrength);

/// <summary>
/// An individual mesh in a model, representing a specific and unique LOD.
/// </summary>
/// <param name="VertexOffset">Offset to the first vertex of this mesh in the vertex array.</param>
/// <param name="VertexCount">Number of vertices in this mesh.</param>
/// <param name="IndexOffset">Offset to the first index of this mesh in the indices array.</param>
/// <param name="IndexCount">Number of indices in this mesh.</param>
/// <param name="BoundsMin"></param>
/// <param name="BoundsMax"></param>
public readonly record struct CKTMesh(int VertexOffset, int VertexCount, int IndexOffset, int IndexCount, Vector3 BoundsMin, Vector3 BoundsMax);

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
public readonly record struct CKTTriangle(int MaterialIndex);

/// <summary>
/// Should always spell "CapriKit.Textureless.Model" for valid files.
/// </summary>
[InlineArray(26)]
public struct FileTypeIdentifier
{
    public char[] Magic;
}

public readonly record struct CKTHeader(FileTypeIdentifier FileType, int FileTypeVersion, string Name, int Materials, int Meshes, int Vertices, int Triangles);

/// <summary>
/// Data for a CapriKit Textureless Model. A 3D model with 1..n LODs where each triangle is assigned a material instead of a texture.
/// Assumes:
/// - Y is up
/// - Units are in meters
/// </summary>
public sealed class CKTModelData
{
    public CKTHeader Header { get; }

    /// <summary>
    /// All meshes in the model. Each mesh represent a different Level-Of-Detail (LOD).
    /// Meshes are ordered from highest to lowest detail.
    /// </summary>
    public CKTMesh[] Meshes { get; }

    /// <summary>
    /// All materials in the model.
    /// </summary>
    public CKTMaterial[] Materials { get; }

    /// <summary>
    /// The vertices. All the vertices that belong to one mesh are unique in the combined value of (Position, Normal).
    /// Multiple vertices with the same position but with a different normal can exist to facilitate sharp corners.
    /// Vertices is one contiguous array of all vertices of all meshes, but vertices are not shared between meshes.
    /// </summary>
    public CKTVertex[] Vertices { get; }

    /// <summary>
    /// The indices that define triangles. Each triangle is defined by three indices.
    /// Indices is one contiguous array of all indices of all meshes, but indices are not shared between meshes.
    /// Index values are relative towards the first vertex that belongs to the model.
    /// Each triangle is defined by three indices, (no triangle fans or other tricks)
    /// </summary>
    public uint[] Indices { get; }

    /// <summary>
    /// Provides extra information for each triangle. For every three indices there is exactly one entry in triangles.
    /// Indices at Indices[3], Indices[4], Indices[5] all refer to the triangle at Triangles[1].
    /// </summary>
    public CKTTriangle[] Triangles { get; }
}

public sealed class CKTModel
{
    private readonly ImmutableStructuredBuffer<CKTMaterial> Materials;
    private readonly ImmutableStructuredBuffer<CKTTriangle> Triangles;
    private readonly ImmutableVertexBuffer<CKTVertex> Vertices;
    private readonly ImmutableIndexBuffer<uint> Indices;

    public CKTModel(Device device, CKTModelData data)
    {
        var name = data.Header.Name;
        Materials = new ImmutableStructuredBuffer<CKTMaterial>(device, data.Materials, $"{name}_materials");
        Triangles = new ImmutableStructuredBuffer<CKTTriangle>(device, data.Triangles, $"{name}_triangles");
        Vertices = new ImmutableVertexBuffer<CKTVertex>(device, data.Vertices, $"{name}_vertices");
        Indices = IndexBuffers.CreateU32Immutable(device, data.Indices, $"{name}_indices");
    }
}
