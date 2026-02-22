using CapriKit.DirectX11.Buffers;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Contexts;

public sealed class InputAssemblerContext : DeviceContextPart
{
    public InputAssemblerContext(ID3D11DeviceContext context)
        : base(context) { }

    public void SetVertexBuffer<T>(VertexBuffer<T> buffer, uint vertexOffset = 0)
        where T : unmanaged
    {
        if (buffer.nativeBuffer == null)
        {
            throw new Exception($"Failed to set uninitialized vertex buffer {buffer.Name}");
        }

        var stride = buffer.PrimitiveSizeInBytes;
        var offset = vertexOffset * stride;
        ID3D11DeviceContext.IASetVertexBuffer(0, buffer.nativeBuffer, stride, offset);
    }

    public void SetIndexBuffer(IndexBufferU16 buffer)
    {
        if (buffer.nativeBuffer == null)
        {
            throw new Exception($"Failed to set uninitialized index buffer {buffer.Name}");
        }

        ID3D11DeviceContext.IASetIndexBuffer(buffer.nativeBuffer, Format.R16_UInt, 0);
    }

    public void SetIndexBuffer(IndexBufferU32 buffer)
    {
        if (buffer.nativeBuffer == null)
        {
            throw new Exception($"Failed to set uninitialized index buffer {buffer.Name}");
        }

        ID3D11DeviceContext.IASetIndexBuffer(buffer.nativeBuffer, Format.R32_UInt, 0);
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
