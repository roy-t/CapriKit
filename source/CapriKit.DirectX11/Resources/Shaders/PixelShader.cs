using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IPixelShader : IDisposable
{
    internal ID3D11PixelShader ID3D11PixelShader { get; set; }

    public void HotSwap(IPixelShader replacement)
    {
        var oldShader = ID3D11PixelShader;
        ID3D11PixelShader = replacement.ID3D11PixelShader;
        oldShader.Dispose();
    }
}

internal sealed class PixelShader : IPixelShader
{
    private ID3D11PixelShader Shader;

    internal PixelShader(ID3D11PixelShader shader)
    {
        Shader = shader;
    }

    ID3D11PixelShader IPixelShader.ID3D11PixelShader
    {
        get { return Shader; }
        set { Shader = value; }
    }
    public void Dispose()
    {
        Shader.Dispose();
    }
}
