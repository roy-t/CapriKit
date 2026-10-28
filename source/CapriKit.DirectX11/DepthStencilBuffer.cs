using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11;

public sealed class DepthStencilBuffer : IDepthStencilView
{
    private readonly ID3D11Texture2D Texture;
    private readonly ID3D11DepthStencilView View;

    public DepthStencilBuffer(Device device, int width, int height, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var textureDescription = new Texture2DDescription(Format.D32_Float, (uint)width, (uint)height, 1u, 1u, BindFlags.DepthStencil);
        Texture = device.ID3D11Device.CreateTexture2D(in textureDescription);
        Texture.DebugName = DebugName.For(Texture, caller: caller, callerFile: callerFile, callerLine: callerLine);

        View = device.ID3D11Device.CreateDepthStencilView(Texture);
        View.DebugName = DebugName.For(View, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    ID3D11DepthStencilView IDepthStencilView.ID3D11DepthStencilView => View;

    public void Dispose()
    {
        View.Dispose();
        Texture.Dispose();
    }
}
