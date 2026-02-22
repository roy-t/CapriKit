using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public interface IComputeShader
{
    internal ID3D11ComputeShader ID3D11ComputeShader { get; }
}

public sealed class ComputeShaderContext : DeviceContextPart
{
    internal ComputeShaderContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetSampler(uint slot, SamplerState sampler)
    {
        ID3D11DeviceContext.CSSetSampler(slot, sampler.State);
    }

    public void SetSamplers(uint startSlot, params SamplerState[] samplers)
    {
        var nativeSamplers = new ID3D11SamplerState[samplers.Length];
        for (var i = 0; i < samplers.Length; i++)
        {
            nativeSamplers[i] = samplers[i].State;
        }

        ID3D11DeviceContext.CSSetSamplers(startSlot, nativeSamplers);
    }

    public void SetShader(IComputeShader? shader)
    {
        ID3D11DeviceContext.CSSetShader(shader?.ID3D11ComputeShader);
    }

    public void SetShaderResource(uint slot, IShaderResourceView? resource)
    {
        ID3D11DeviceContext.CSSetShaderResource(slot, resource?.ID3D11ShaderResourceView);
    }

    public void SetConstantBuffer<T>(uint slot, ConstantBuffer<T> buffer)
        where T : unmanaged
    {
        ID3D11DeviceContext.CSSetConstantBuffer(slot, buffer?.nativeBuffer);
    }

    public void SetUnorderedAccessView(uint slot, IUnorderedAccessView view)
    {
        ID3D11DeviceContext.CSSetUnorderedAccessView(slot, view.ID3D11UnorderedAccessView);
    }

    public void SetUnorderedAccessView(uint slot, IUnorderedAccessViewArray views, int mipMapSlice)
    {
        ID3D11DeviceContext.CSSetUnorderedAccessView(slot, views.ID3D11UnorderedAccessViews[mipMapSlice]);
    }

    public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
    {
        ID3D11DeviceContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }
}
