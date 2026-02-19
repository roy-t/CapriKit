using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources;
using System.Numerics;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace CapriKit.DirectX11.Contexts;

public sealed class ImmediateDeviceContext : DeviceContext
{
    public ImmediateDeviceContext(ID3D11DeviceContext context)
        : base(context)
    {
        Name = DebugName.For(this);
    }

    public override string Name { get; }

    public void ExecuteCommandList(ICommandList commandList)
    {
        ID3D11DeviceContext.ExecuteCommandList(commandList.ID3D11CommandList, false);
    }
    public override void Dispose()
    {
        // Do nothing, the immediate device context is owned by the Device class
        GC.SuppressFinalize(this);
    }

}


public sealed class DeferredDeviceContext : DeviceContext
{
    public DeferredDeviceContext(ID3D11DeviceContext context, string? nameHint = null)
        : base(context)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }

    public ICommandList FinishCommandList()
    {
        var commandList = ID3D11DeviceContext.FinishCommandList(false);
        commandList.DebugName = DebugName.For(commandList, Name);
        return new CommandList(commandList);
    }

    public override void Dispose()
    {
        ID3D11DeviceContext.Dispose();
        GC.SuppressFinalize(this);
    }
}


public abstract class DeviceContext : IDisposable
{
    internal DeviceContext(ID3D11DeviceContext context)
    {
        ID3D11DeviceContext = context;

        IA = new InputAssemblerContext(context);
        VS = new VertexShaderContext(context);
        RS = new RasterizerContext(context);
        PS = new PixelShaderContext(context);
        OM = new OutputMergerContext(context);
        CS = new ComputeShaderContext(context);
    }

    internal ID3D11DeviceContext ID3D11DeviceContext { get; }

    public abstract string Name { get; }

    public InputAssemblerContext IA { get; }
    public VertexShaderContext VS { get; }
    public RasterizerContext RS { get; }
    public PixelShaderContext PS { get; }
    public OutputMergerContext OM { get; }
    public ComputeShaderContext CS { get; }

    public void Clear(IDepthStencilView view, float depth = 0, byte stencil = 0)
    {
        ID3D11DeviceContext.ClearDepthStencilView(view.ID3D11DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, depth, stencil);
    }

    public void Clear(IRenderTargetView view, float r, float g, float b, float a)
    {
        var color = new Color4(r, g, b, a);
        ID3D11DeviceContext.ClearRenderTargetView(view.ID3D11RenderTargetView, in color);
    }

    public void Clear(IUnorderedAccessView view, Vector4 values)
    {
        ID3D11DeviceContext.ClearUnorderedAccessView(view.ID3D11UnorderedAccessView, values);
    }

    public void Clear(IUnorderedAccessView view, int x, int y, int z, int w)
    {
        var values = new Int4(x, y, z, w);
        ID3D11DeviceContext.ClearUnorderedAccessView(view.ID3D11UnorderedAccessView, values);
    }

    /// <summary>
    /// Draw non-indexed, non-instanced primitives.
    /// </summary>
    /// <param name="vertexCount">Number of vertices to draw.</param>
    /// <param name="vertexStartLocation">Index of the first vertex, which is usually an offset in a vertex buffer.</param>
    public void Draw(uint vertexCount, uint vertexStartLocation = 0)
    {
        ID3D11DeviceContext.Draw(vertexCount, vertexStartLocation);
    }

    /// <summary>
    /// Draw indexed, non-instanced primitives.
    /// </summary>
    /// <param name="indexCount">Number of indices to draw.</param>
    /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
    /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
    public void DrawIndexed(uint indexCount, uint startIndexLocation = 0, int baseVertexLocation = 0)
    {
        ID3D11DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
    }

    /// <summary>
    /// Draw non-indexed, instanced primitives.
    /// </summary>
    /// <param name="vertexCountPerInstance">Number of vertices to draw.</param>
    /// <param name="instanceCount">Number of instances to draw.</param>
    /// <param name="startVertexLocation">Index of the first vertex.</param>
    /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
    public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation = 0, uint startInstanceLocation = 0)
    {
        ID3D11DeviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
    }

    /// <summary>
    /// Draw indexed, instanced primitives.
    /// </summary>
    /// <param name="indexCountPerInstance">Number of indices read from the index buffer for each instance.</param>
    /// <param name="instanceCount">Number of instances to draw.</param>
    /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
    /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
    /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
    public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation = 0, int baseVertexLocation = 0, uint startInstanceLocation = 0)
    {
        ID3D11DeviceContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    public abstract void Dispose();
}
