using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using System.Drawing;

namespace CapriKit.DirectX11.Contexts;

public static class DeviceContextExtensions
{
    /// <summary>
    /// Sets the most common device states for general purpose rendering
    /// </summary>    
    public static void Setup(this DeviceContext deviceContext, IInputLayout inputLayout, PrimitiveTopology primitiveTopology, IVertexShader vertexShader, RasterizerState rasterizerState, in Rectangle viewport, IPixelShader pixelShader, BlendState blendState, DepthStencilState depthStencilState)
    {
        deviceContext.IA.SetInputLayout(inputLayout);
        deviceContext.IA.SetPrimitiveTopology(primitiveTopology);

        deviceContext.VS.SetShader(vertexShader);

        deviceContext.RS.SetRasterizerState(rasterizerState);

        deviceContext.RS.SetScissorRect(in viewport);
        deviceContext.RS.SetViewport(in viewport);

        deviceContext.PS.SetShader(pixelShader);

        deviceContext.OM.SetBlendState(blendState);
        deviceContext.OM.SetDepthStencilState(depthStencilState);
    }

    /// <summary>
    /// Sets the most common device states for rendering using the 'full screen triangle' technique. Useful for most post processing effects.
    /// Assumes a vertex shader that creates the vertices of a triangle that covers the entire viewport based on vertex ids.    
    /// </summary>    
    public static void SetupFullScreenTriangle(this DeviceContext deviceContext, IVertexShader vertexShader, RasterizerState cullNone, in Rectangle viewport, IPixelShader pixelShader, BlendState blendState, DepthStencilState depthStencilState)
    {
        deviceContext.IA.SetInputLayout(null);
        deviceContext.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        deviceContext.VS.SetShader(vertexShader);

        deviceContext.RS.SetRasterizerState(cullNone);

        deviceContext.RS.SetScissorRect(in viewport);
        deviceContext.RS.SetViewport(in viewport);

        deviceContext.PS.SetShader(pixelShader);

        deviceContext.OM.SetBlendState(blendState);
        deviceContext.OM.SetDepthStencilState(depthStencilState);
    }
}
