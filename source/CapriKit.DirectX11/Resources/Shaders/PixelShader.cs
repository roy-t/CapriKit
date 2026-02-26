using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public sealed class PixelShader : IPixelShader
{
    private readonly ID3D11PixelShader Shader;

    internal PixelShader(ID3D11PixelShader shader)
    {
        Shader = shader;
    }

    ID3D11PixelShader IPixelShader.ID3D11PixelShader => Shader;
}
