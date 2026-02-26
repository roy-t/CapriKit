using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IPixelShader : IDisposable
{
    internal ID3D11PixelShader ID3D11PixelShader { get; }
}

public sealed class PixelShader : IPixelShader
{
    private readonly ID3D11PixelShader Shader;

    internal PixelShader(ID3D11PixelShader shader)
    {
        Shader = shader;
    }

    ID3D11PixelShader IPixelShader.ID3D11PixelShader => Shader;

    public void Dispose()
    {
        Shader.Dispose();
    }
}
