using CapriKit.DirectX11.Contexts.States;
using System.Drawing;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class RasterizerContext : DeviceContextPart
{
    internal RasterizerContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetRasterizerState(RasterizerState state)
    {
        ID3D11DeviceContext.RSSetState(state.State);
    }

    public void SetScissorRect(int x, int y, int width, int height)
    {
        ID3D11DeviceContext.RSSetScissorRect(x, y, width, height);        
    }

    public void SetScissorRect(in Rectangle rectangle)
    {
        SetScissorRect(rectangle.Left, rectangle.Top, rectangle.Width, rectangle.Height);
    }

    public void SetViewport(float x, float y, float width, float height, float minDepth = 0.0f, float maxDepth = 1.0f)
    {
        ID3D11DeviceContext.RSSetViewport(x, y, width, height, minDepth, maxDepth);
    }

    public void SetViewport(in Rectangle rectangle, float minDepth = 0.0f, float maxDepth = 1.0f)
    {
        SetViewport(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, minDepth, maxDepth);
    }
}
