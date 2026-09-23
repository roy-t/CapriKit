using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources.Views;
using System.Diagnostics.CodeAnalysis;
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
/// <param name="TriangleOffset">Offset to the first triangle of this mesh in the triangles array</param>
/// <param name="TriangleCount">Number of triangles in this mesh</param>
/// <param name="BoundsMin"></param>
/// <param name="BoundsMax"></param>
public readonly record struct CKTMesh(int VertexOffset, int VertexCount, int IndexOffset, int IndexCount, int TriangleOffset, int TriangleCount, Vector3 BoundsMin, Vector3 BoundsMax);

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
    public char Character;
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
    /// The individual meshes in the model. Each mesh represent a different Level-Of-Detail (LOD).
    /// Meshes are ordered from highest to lowest detail.
    /// </summary>
    public CKTMesh[] Meshes { get; }

    /// <summary>
    /// The materials used in the model. Materials are shared between meshes so when rendering a mesh you need
    /// access to the entire array.
    /// </summary>
    public CKTMaterial[] Materials { get; }

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
    /// Use SV_PrimitiveID in your shader and add the offset appropriate for the mesh you are rendering.
    /// </summary>
    public CKTTriangle[] Triangles { get; }
}

public sealed class CKTModel : IDisposable
{
    private ImmutableStructuredBuffer<CKTMaterial> materials;
    private ImmutableStructuredBuffer<CKTTriangle> triangles;
    private ImmutableVertexBuffer<CKTVertex> vertices;
    private ImmutableIndexBuffer<uint> indices;

    private IShaderResourceView materialsView;
    private IShaderResourceView trianglesView;

    private CKTMesh[] meshes;

    public CKTModel(Device device, CKTModelData data)
    {
        Name = data.Header.Name;
        Initialize(device, data);
    }

    public string Name { get; }

    public void Draw(DeviceContext context, int lod)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lod, meshes.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(lod);

        // TODO: instead of actually drawing here make the information easy to access.
        var mesh = meshes[lod];

        var vertexOffset = mesh.VertexOffset;
        var indexOffset = mesh.IndexOffset;
        var indexCount = mesh.IndexCount;
        var triangleOffset = mesh.TriangleOffset;

        context.IA.SetVertexBuffer(vertices);
        context.IA.SetIndexBuffer(indices);
        context.PS.SetShaderResource(0, materialsView);
        context.PS.SetShaderResource(1, trianglesView);

        // TODO: set triangleOffset in the shader's cbuffer so that material lookup is materials[triangles[SV_PrimitiveID + triangleOffset]
        // pass mesh.VertexOffset so that an index with value 0 points at vertex[0 + vertexOffset] points to the first vertex that belongs to this mesh
        // not the first vertex in the entire vertex array.
        context.DrawIndexed((uint)indexCount, (uint)indexOffset, vertexOffset);
    }

    internal void HotReload(Device device, CKTModelData data)
    {
        Dispose();
        Initialize(device, data);
    }

    [MemberNotNull(nameof(materials), nameof(triangles), nameof(vertices), nameof(indices), nameof(meshes), nameof(materialsView), nameof(trianglesView))]
    private void Initialize(Device device, CKTModelData data)
    {
        materials = new ImmutableStructuredBuffer<CKTMaterial>(device, data.Materials, $"{Name}_materials");
        triangles = new ImmutableStructuredBuffer<CKTTriangle>(device, data.Triangles, $"{Name}_triangles");
        vertices = new ImmutableVertexBuffer<CKTVertex>(device, data.Vertices, $"{Name}_vertices");
        indices = IndexBuffers.CreateU32Immutable(device, data.Indices, $"{Name}_indices");
        materialsView = materials.CreateShaderResourceView(device);
        trianglesView = triangles.CreateShaderResourceView(device);
        meshes = data.Meshes;
    }

    public void Dispose()
    {
        materialsView.Dispose();
        trianglesView.Dispose();
        materials.Dispose();
        triangles.Dispose();
        vertices.Dispose();
        indices.Dispose();
        meshes = [];
    }
}
