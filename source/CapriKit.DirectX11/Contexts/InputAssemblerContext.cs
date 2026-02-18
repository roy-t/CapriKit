using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Contexts;

public sealed class InputAssemblerContext : DeviceContextPart
{
    public InputAssemblerContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetVertexBuffer<T>(VertexBuffer<T> buffer, uint vertexOffset = 0)
        where T : unmanaged
    {
        if (buffer.Buffer == null)
        {
            throw new Exception($"Failed to set uninitialized vertex buffer {buffer.Name}");
        }

        var stride = buffer.PrimitiveSizeInBytes;
        var offset = vertexOffset * stride;
        ID3D11DeviceContext.IASetVertexBuffer(0, buffer.Buffer, stride, offset);
    }

    public void SetIndexBuffer<T>(IndexBuffer<T> buffer)
        where T : unmanaged
    {
        if (buffer.Buffer == null)
        {
            throw new Exception($"Failed to set uninitialized index buffer {buffer.Name}");
        }

        ID3D11DeviceContext.IASetIndexBuffer(buffer.Buffer, buffer.Format, 0);
    }

    public void SetInputLayout(InputLayout? inputLayout)
    {
        ID3D11DeviceContext.IASetInputLayout(inputLayout?.ID3D11InputLayout);
    }

    // TODO: this leaks PrimitiveTopology to the outside world
    public void SetPrimitiveTopology(PrimitiveTopology topology)
    {
        ID3D11DeviceContext.IASetPrimitiveTopology(topology);
    }
}
