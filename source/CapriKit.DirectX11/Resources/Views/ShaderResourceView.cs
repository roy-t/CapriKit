using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Views;

public interface IShaderResourceView : IDisposable
{
    internal ID3D11ShaderResourceView ID3D11ShaderResourceView { get; }
}

internal sealed class ShaderResourceView : IShaderResourceView
{
    private readonly ID3D11ShaderResourceView Value;

    internal ShaderResourceView(ID3D11ShaderResourceView view)
    {
        Value = view;
    }

    ID3D11ShaderResourceView IShaderResourceView.ID3D11ShaderResourceView => Value;

    public void Dispose()
    {
        Value.Dispose();
    }
}
