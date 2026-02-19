using CapriKit.DirectX11.Contexts.States;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class OutputMergerContext : DeviceContextPart
{
    internal OutputMergerContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetBlendState(BlendState state)
    {
        ID3D11DeviceContext.OMSetBlendState(state.ID3D11BlendState);
    }

    public void SetDepthStencilState(DepthStencilState state)
    {
        ID3D11DeviceContext.OMSetDepthStencilState(state.ID3D11DepthStencilState);
    }

    public void SetRenderTarget(IRenderTargetView renderTarget, IDepthStencilView? depthStencil = null)
    {
        ID3D11DeviceContext.OMSetRenderTargets(renderTarget.ID3D11RenderTargetView, depthStencil?.ID3D11DepthStencilView);
    }

    public void SetRenderTargets(IRenderTargetViewArray renderTargets, IDepthStencilView? depthStencil = null)
    {
        ID3D11DeviceContext.OMSetRenderTargets(renderTargets.ID3D11RenderTargetViews, depthStencil?.ID3D11DepthStencilView);
    }

    public void SetRenderTarget(IDepthStencilView depthStencil)
    {
#nullable disable        
        ID3D11DeviceContext.OMSetRenderTargets((ID3D11RenderTargetView)null!, depthStencil.ID3D11DepthStencilView);
#nullable restore
    }

    public void SetRenderTarget(IDepthStencilViewArray depthStencils, int slice)
    {
#nullable disable
        ID3D11DeviceContext.OMSetRenderTargets((ID3D11RenderTargetView)null, depthStencils.ID3D11DepthStencilViews[slice]);
#nullable restore
    }
}
