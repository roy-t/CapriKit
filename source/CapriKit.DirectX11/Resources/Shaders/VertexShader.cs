using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IVertexShader : IDisposable
{
    internal ID3D11VertexShader ID3D11VertexShader { get; }

    IInputLayout CreateInputLayout(Device device, InputElementDescription[] elements); // TODO: How to do this without exposing types from Vortice?
}

internal sealed class VertexShader : IVertexShader
{
    private readonly byte[] Blob;
    private readonly ID3D11VertexShader Shader;

    internal VertexShader(byte[] blob, ID3D11VertexShader shader)
    {
        Blob = blob;
        Shader = shader;
    }

    ID3D11VertexShader IVertexShader.ID3D11VertexShader => Shader;

    public IInputLayout CreateInputLayout(Device device, InputElementDescription[] elements)
    {
        var inputLayout = device.ID3D11Device.CreateInputLayout(elements, Blob);
        return new InputLayout(inputLayout);
    }

    public void Dispose()
    {
        Shader.Dispose();
    }
}
