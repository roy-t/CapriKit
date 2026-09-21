using System.Numerics;
using Vortice.Mathematics;

namespace CapriKit.AssetPipeline.DirectX11.Models;

// TODO: what about names of individual meshes?
// TODO: define that Y is up is assumed
// TODO: header is not blittaable, maybe just take 8 magic bytes?
// TODO: require/validate that exporter dedubed vertices
// TODO: validate invariants like (indices.Length == Triangles.Length *3)
// TODO: 16 bit or 32 bit indices support
// TODO: Blender: applied modifiers, split corner (corner_normals), make material indexes global, scene unit == 1.0 (1meter)


/// <summary>
/// Material properties.
/// </summary>
/// <param name="BaseColor">Base color in linear space.</param>
/// <param name="Metallic">Zero (0) for dielectric materials One (1) for conductors.</param>
/// <param name="Roughness">Roughness in [0..1].</param>
/// <param name="EmissionColor">Emission color in linear space.</param>
/// <param name="EmissionStrength">Strength of the emitted light in arbitrary units [0..inf).</param>
public readonly record struct SMaterial(Color3 BaseColor, float Metallic, float Roughness, Color3 EmissionColor, float EmissionStrength);

/// <summary>
/// An individual mesh in a model, representing a specific and unique LOD.
/// </summary>
/// <param name="VertexOffset">Offset to the first vertex of this mesh in the vertex array.</param>
/// <param name="VertexCount">Number of vertices in this mesh.</param>
/// <param name="IndexOffset">Offset to the first index of this mesh in the indices array.</param>
/// <param name="IndexCount">Number of indices in this mesh.</param>
/// <param name="LOD">Level of detail from zero (0) most detailed to n (simplest).</param>
/// <param name="BoundsMin"></param>
/// <param name="BoundsMax"></param>
public readonly record struct SMesh(int VertexOffset, int VertexCount, int IndexOffset, int IndexCount, int LOD, Vector3 BoundsMin, Vector3 BoundsMax);

/// <summary>
/// A vertex with a position and normal
/// </summary>
/// <param name="Position">Position in meters from an arbitrary origin</param>
/// <param name="Normal">Normalized normal for lighting calculations</param>
public readonly record struct SVertex(Vector3 Position, Vector3 Normal);

/// <summary>
/// A triangle with an assigned material.
/// </summary>
/// <param name="MaterialIndex">Index into the materials array</param>
public readonly record struct STriangle(int MaterialIndex);

public readonly record struct Header(Guid FileType, int FileTypeVersion, string Name, int Materials, int Meshes, int Vertices, int Triangles);

public sealed class BinaryModel
{
    /// <summary>
    /// All meshes in the model.
    /// </summary>
    private readonly SMesh[] Meshes;

    /// <summary>
    /// All materials in the model.
    /// </summary>
    private readonly SMaterial[] Materials;

    /// <summary>
    /// The vertices. All the vertices that belong to one mesh are unique in the combined value of (Position, Normal).
    /// Multiple vertices with the same position but with a different normal can exist to facilitate sharp corners.
    /// Vertices is one contiguous array of all vertices of all meshes, but vertices are not shared between meshes.
    /// </summary>
    private readonly SVertex[] Vertices;

    /// <summary>
    /// The indices that define triangles. Each triangle is defined by three indices.
    /// Indices is one contiguous array of all indices of all meshes, but indices are not shared between meshes.
    /// Index values are relative towards the first vertex that belongs to the model.
    /// Each triangle is defined by three indices, (no triangle fans or other tricks)
    /// </summary>
    private readonly int[] Indices;

    /// <summary>
    /// Provides extra information for each triangle. For every three indices there is exactly one entry in triangles.
    /// Indices at Indices[3], Indices[4], Indices[5] all refer to the triangle at Triangles[1].
    /// </summary>
    private readonly STriangle[] Triangles;
}
