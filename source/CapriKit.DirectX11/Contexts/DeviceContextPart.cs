using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public abstract class DeviceContextPart
{
    protected readonly ID3D11DeviceContext ID3D11DeviceContext;

    protected DeviceContextPart(ID3D11DeviceContext context)
    {
        ID3D11DeviceContext = context;
    }
}
