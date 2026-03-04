using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources;

public interface IUnorderedAccessViewArray : IDisposable
{
    internal ID3D11UnorderedAccessView[] ID3D11UnorderedAccessViews { get; }
}

public interface IRenderTargetView : IDisposable
{
    internal ID3D11RenderTargetView ID3D11RenderTargetView { get; }
}

public interface IRenderTargetViewArray : IDisposable
{
    internal ID3D11RenderTargetView[] ID3D11RenderTargetViews { get; }
}

public interface IDepthStencilView : IDisposable
{
    internal ID3D11DepthStencilView ID3D11DepthStencilView { get; }
}

public interface IDepthStencilViewArray : IDisposable
{
    internal ID3D11DepthStencilView[] ID3D11DepthStencilViews { get; }
}


