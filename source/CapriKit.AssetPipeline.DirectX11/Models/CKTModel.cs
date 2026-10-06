using CapriKit.DirectX11;
using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Resources.Views;
using CapriKit.Geometry;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.AssetPipeline.DirectX11.Models;

public sealed class CKTModel : IDisposable
{
    private ImmutableStructuredBuffer<CKTMaterial> materials;
    private ImmutableStructuredBuffer<CKTTriangle> triangles;
    private ImmutableVertexBuffer<CKTVertex> vertices;
    private ImmutableIndexBuffer<uint> indices;
    private IShaderResourceView materialsView;
    private IShaderResourceView[] triangleViews;

    private CKTMesh[] meshes;

    public CKTModel(Device device, CKTModelData data)
    {
        Name = data.Header.Name;
        Initialize(device, data);
    }

    public string Name { get; }   

    public void DrawIndexed(DeviceContext context, uint materialSRVSlot, uint triangleSRVSlot, int lod = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lod, meshes.Length);
        ArgumentOutOfRangeException.ThrowIfNegative(lod);

        var mesh = meshes[lod];
        var vertexOffset = mesh.VertexOffset;
        var indexOffset = mesh.IndexOffset;
        var indexCount = mesh.IndexCount;

        context.IA.SetVertexBuffer(vertices);
        context.IA.SetIndexBuffer(indices);
        context.PS.SetShaderResource(materialSRVSlot, materialsView);
        context.PS.SetShaderResource(triangleSRVSlot, triangleViews[lod]);

        context.DrawIndexed(indexCount, indexOffset, (int)vertexOffset);
    }

    internal void HotReload(Device device, CKTModelData data)
    {
        Dispose();
        Initialize(device, data);
    }

    [MemberNotNull(nameof(materials), nameof(triangles), nameof(vertices), nameof(indices), nameof(meshes), nameof(materialsView), nameof(triangleViews))]
    private void Initialize(Device device, CKTModelData data)
    {
        materials = new ImmutableStructuredBuffer<CKTMaterial>(device, data.Materials, $"{Name}_materials");
        triangles = new ImmutableStructuredBuffer<CKTTriangle>(device, data.Triangles, $"{Name}_triangles");
        vertices = new ImmutableVertexBuffer<CKTVertex>(device, data.Vertices, $"{Name}_vertices");
        indices = IndexBuffers.CreateU32Immutable(device, data.Indices, $"{Name}_indices");
        materialsView = materials.CreateShaderResourceView(device);

        triangleViews = new IShaderResourceView[data.Meshes.Length];
        for (var i = 0; i < triangleViews.Length; i++)
        {
            var mesh = data.Meshes[i];
            triangleViews[i] = triangles.CreateShaderResourceView(device, mesh.TriangleOffset, mesh.TriangleCount);
        }

        meshes = data.Meshes;
    }

    public void Dispose()
    {
        materialsView.Dispose();
        for (var i = 0; i < triangleViews.Length; i++)
        {
            triangleViews[i].Dispose();
        }

        materials.Dispose();
        triangles.Dispose();
        vertices.Dispose();
        indices.Dispose();

        meshes = [];
    }
}
