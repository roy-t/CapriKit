using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class PixelShaderContext : DeviceContextPart
{
    internal PixelShaderContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetSampler(uint slot, SamplerState sampler)
    {
        ID3D11DeviceContext.PSSetSampler(slot, sampler.State);        
    }

    public void SetSamplers(uint startSlot, params SamplerState[] samplers)
    {
        var nativeSamplers = new ID3D11SamplerState[samplers.Length];
        for (var i = 0; i < samplers.Length; i++)
        {
            nativeSamplers[i] = samplers[i].State;
        }

        ID3D11DeviceContext.PSSetSamplers(startSlot, nativeSamplers);
    }

    public void SetShader(IPixelShader? shader)
    {
        ID3D11DeviceContext.PSSetShader(shader?.ID3D11PixelShader, null, 0);
    }

    public void SetShaderResource(uint slot, IShaderResourceView resource)
    {
        if (resource == null)
        {
            ID3D11DeviceContext.PSUnsetShaderResource(slot);
        }
        else
        {
            ID3D11DeviceContext.PSSetShaderResource(slot, resource.ID3D11ShaderResourceView);
        }
    }

    public void SetConstantBuffer<T>(uint slot, ConstantBuffer<T> buffer)
        where T : unmanaged
    {
        ID3D11DeviceContext.PSSetConstantBuffer(slot, buffer.nativeBuffer);
    }
}
