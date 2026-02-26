using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Resources;
using CapriKit.DirectX11.Resources.Shaders;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class VertexShaderContext : DeviceContextPart
{
    internal VertexShaderContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetConstantBuffer<T>(uint slot, ConstantBuffer<T> buffer)
        where T : unmanaged
    {
        ID3D11DeviceContext.VSSetConstantBuffer(slot, buffer.nativeBuffer);
    }

    public void SetSampler(uint slot, SamplerState sampler)
    {
        ID3D11DeviceContext.VSSetSampler(slot, sampler.State);
    }

    public void SetShader(IVertexShader shader)
    {
        ID3D11DeviceContext.VSSetShader(shader.ID3D11VertexShader);
    }

    public void SetShaderResource(uint slot, IShaderResourceView view)
    {
        ID3D11DeviceContext.VSSetShaderResource(slot, view.ID3D11ShaderResourceView);
    }
}
