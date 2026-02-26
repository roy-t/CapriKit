using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public sealed class VertexShader : IVertexShader
{
    private readonly Blob Blob;
    private readonly ID3D11VertexShader Shader;

    internal VertexShader(Blob blob, ID3D11VertexShader shader)
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
}
