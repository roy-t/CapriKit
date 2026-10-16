using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11;

public sealed class DepthStencilBuffer : IDepthStencilView
{
    private readonly ID3D11Texture2D Texture;
    private readonly ID3D11DepthStencilView View;

    public DepthStencilBuffer(Device device, int width, int height)
    {
        var textureDescription = new Texture2DDescription(Format.D32_Float, (uint)width, (uint)height, 1u, 1u, BindFlags.DepthStencil);
        Texture = device.ID3D11Device.CreateTexture2D(in textureDescription);
        Texture.DebugName = DebugName.For(Texture, "depth");

        View = device.ID3D11Device.CreateDepthStencilView(Texture);
        View.DebugName = DebugName.For(View, "depth");
    }

    ID3D11DepthStencilView IDepthStencilView.ID3D11DepthStencilView => View;

    public void Dispose()
    {
        View.Dispose();
        Texture.Dispose();
    }
}
