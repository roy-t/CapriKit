using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources;

public interface IInputLayout : IDisposable
{
    internal ID3D11InputLayout ID3D11InputLayout { get; }
}

internal sealed class InputLayout : IInputLayout
{
    private readonly ID3D11InputLayout Value;

    internal InputLayout(ID3D11InputLayout inputLayout)
    {
        Value = inputLayout;
    }

    ID3D11InputLayout IInputLayout.ID3D11InputLayout => Value;

    public void Dispose()
    {
        Value.Dispose();
    }
}
