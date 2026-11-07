using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Debug;

/// <summary>
/// Counts how much work each stage of the GPU pipeline did. Unlike timings these numbers are exact.
/// </summary>
/// <param name="InputAssemblerVertices">Vertices read by the input assembler.</param>
/// <param name="InputAssemblerPrimitives">Primitives read by the input assembler.</param>
/// <param name="VertexShaderInvocations">Vertex shader invocations, lower than the vertex count if the post-transform cache hits.</param>
/// <param name="GeometryShaderInvocations">Geometry shader invocations.</param>
/// <param name="GeometryShaderPrimitives">Primitives output by the geometry shader.</param>
/// <param name="RasterizerInvocations">Primitives sent to the rasterizer.</param>
/// <param name="RasterizerPrimitives">Primitives that survived clipping and culling and were rendered.</param>
/// <param name="PixelShaderInvocations">Pixel shader invocations, includes pixels that are later rejected by the depth test.</param>
/// <param name="HullShaderInvocations">Hull shader invocations.</param>
/// <param name="DomainShaderInvocations">Domain shader invocations.</param>
/// <param name="ComputeShaderInvocations">Compute shader invocations, counted per thread.</param>
public readonly record struct PipelineStatistics(
    ulong InputAssemblerVertices,
    ulong InputAssemblerPrimitives,
    ulong VertexShaderInvocations,
    ulong GeometryShaderInvocations,
    ulong GeometryShaderPrimitives,
    ulong RasterizerInvocations,
    ulong RasterizerPrimitives,
    ulong PixelShaderInvocations,
    ulong HullShaderInvocations,
    ulong DomainShaderInvocations,
    ulong ComputeShaderInvocations)
{
    public static PipelineStatistics operator +(PipelineStatistics a, PipelineStatistics b)
    {
        return new PipelineStatistics(
            a.InputAssemblerVertices + b.InputAssemblerVertices,
            a.InputAssemblerPrimitives + b.InputAssemblerPrimitives,
            a.VertexShaderInvocations + b.VertexShaderInvocations,
            a.GeometryShaderInvocations + b.GeometryShaderInvocations,
            a.GeometryShaderPrimitives + b.GeometryShaderPrimitives,
            a.RasterizerInvocations + b.RasterizerInvocations,
            a.RasterizerPrimitives + b.RasterizerPrimitives,
            a.PixelShaderInvocations + b.PixelShaderInvocations,
            a.HullShaderInvocations + b.HullShaderInvocations,
            a.DomainShaderInvocations + b.DomainShaderInvocations,
            a.ComputeShaderInvocations + b.ComputeShaderInvocations);
    }

    internal static PipelineStatistics From(in QueryDataPipelineStatistics data)
    {
        return new PipelineStatistics(
            data.IAVertices,
            data.IAPrimitives,
            data.VSInvocations,
            data.GSInvocations,
            data.GSPrimitives,
            data.CInvocations,
            data.CPrimitives,
            data.PSInvocations,
            data.HSInvocations,
            data.DSInvocations,
            data.CSInvocations);
    }
}
