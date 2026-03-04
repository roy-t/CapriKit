using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Views;

public interface IUnorderedAccessView : IDisposable
{
    internal ID3D11UnorderedAccessView ID3D11UnorderedAccessView { get; }
}

internal sealed class UnorderedAccessView : IUnorderedAccessView
{
    private readonly ID3D11UnorderedAccessView Value;

    internal UnorderedAccessView(ID3D11UnorderedAccessView view)
    {
        Value = view;
    }

    ID3D11UnorderedAccessView IUnorderedAccessView.ID3D11UnorderedAccessView => Value;

    public void Dispose()
    {
        Value.Dispose();
    }
}
