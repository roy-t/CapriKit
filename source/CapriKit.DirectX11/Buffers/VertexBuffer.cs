using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public sealed class VertexBuffer<T> : DeviceBuffer<T>
    where T : unmanaged
{
    public VertexBuffer(Device device, string? nameHint = null)
        : base(device)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }

    protected override ID3D11Buffer CreateBuffer(uint sizeInBytes)
    {
        var description = new BufferDescription()
        {
            Usage = ResourceUsage.Dynamic,
            ByteWidth = sizeInBytes,
            BindFlags = BindFlags.VertexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            StructureByteStride = PrimitiveSizeInBytes
        };

        return Device.CreateBuffer(description);
    }
}
