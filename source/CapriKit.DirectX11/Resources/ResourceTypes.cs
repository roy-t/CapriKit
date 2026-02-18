using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources;

public interface IComputeShader
{
    internal ID3D11ComputeShader ID3D11ComputeShader { get; }
}

public interface IShaderResourceView
{
    internal ID3D11ShaderResourceView ID3D11ShaderResourceView { get; }
}


public interface IUnorderedAccessView
{
    internal ID3D11UnorderedAccessView ID3D11UnorderedAccessView { get; }
}

public interface IUnorderedAccessViewArray
{
    internal ID3D11UnorderedAccessView[] ID3D11UnorderedAccessViews { get; }
}

public interface InputLayout
{
    internal ID3D11InputLayout ID3D11InputLayout { get; }
}

public interface IRenderTargetView
{
    internal ID3D11RenderTargetView ID3D11RenderTargetView { get; }
}

public interface IRenderTargetViewArray
{
    internal ID3D11RenderTargetView[] ID3D11RenderTargetViews { get; }
}

public interface IDepthStencilView
{
    internal ID3D11DepthStencilView ID3D11DepthStencilView { get; }
}

public interface IDepthStencilViewArray
{
    internal ID3D11DepthStencilView[] ID3D11DepthStencilViews { get; }
}

public interface IPixelShader
{
    internal ID3D11PixelShader ID3D11PixelShader { get; }
}

public interface IVertexShader
{
    internal ID3D11VertexShader ID3D11VertexShader { get; }
}
