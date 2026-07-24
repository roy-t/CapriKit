using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Resources.Shaders;

public interface IVertexShader : IDisposable
{
    internal byte[] Blob { get; set; }
    internal ID3D11VertexShader ID3D11VertexShader { get; set; }

    IInputLayout CreateInputLayout(Device device, InputElementDescription[] elements);

    public void HotSwap(IVertexShader newParts)
    {
        Blob = newParts.Blob;

        var oldShader = ID3D11VertexShader;
        ID3D11VertexShader = newParts.ID3D11VertexShader;
        oldShader.Dispose();
    }
}

internal sealed class VertexShader : IVertexShader
{
    private byte[] blob;
    private ID3D11VertexShader shader;

    internal VertexShader(byte[] blob, ID3D11VertexShader shader)
    {
        this.blob = blob;
        this.shader = shader;
    }

    byte[] IVertexShader.Blob
    {
        get { return blob; }
        set { blob = value; }
    }

    ID3D11VertexShader IVertexShader.ID3D11VertexShader
    {
        get { return shader; }
        set { shader = value; }
    }

    public IInputLayout CreateInputLayout(Device device, InputElementDescription[] elements)
    {
        var inputLayout = device.ID3D11Device.CreateInputLayout(elements, blob);
        return new InputLayout(inputLayout);
    }

    public void Dispose()
    {
        shader.Dispose();
    }
}
