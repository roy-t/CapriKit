using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class InputAssemblerContext : DeviceContextPart
{
    public InputAssemblerContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetVertexBuffer<T>(IVertexBuffer<T> buffer, uint vertexOffset = 0)
        where T : unmanaged
    {
        if (buffer.ID3D11Buffer == null)
        {
            throw new Exception($"Failed to set uninitialized vertex buffer {buffer.Name}");
        }

        var stride = buffer.PrimitiveSizeInBytes;
        var offset = vertexOffset * stride;
        ID3D11DeviceContext.IASetVertexBuffer(0, buffer.ID3D11Buffer, stride, offset);
    }

    public void SetIndexBuffer<T>(IIndexBuffer<T> buffer)
        where T : unmanaged
    {
        if (buffer.ID3D11Buffer == null)
        {
            throw new Exception($"Failed to set uninitialized index buffer {buffer.Name}");
        }

        ID3D11DeviceContext.IASetIndexBuffer(buffer.ID3D11Buffer, buffer.Format, 0);
    }

    public void SetInputLayout(IInputLayout? inputLayout)
    {
        ID3D11DeviceContext.IASetInputLayout(inputLayout?.ID3D11InputLayout);
    }

    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        ID3D11DeviceContext.IASetPrimitiveTopology((Vortice.Direct3D.PrimitiveTopology)topology);
    }
}
