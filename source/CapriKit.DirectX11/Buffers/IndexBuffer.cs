using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Buffers;

public sealed class IndexBufferU16 : IndexBuffer<ushort>
{
    public IndexBufferU16(Device device, string? nameHint = null) : base(device, Format.R16_UInt)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}

public sealed class IndexBufferU32 : IndexBuffer<uint>
{
    public IndexBufferU32(Device device, string? nameHint = null) : base(device, Format.R32_UInt)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}

public abstract class IndexBuffer<T> : DeviceBuffer<T>
    where T : unmanaged
{
    internal IndexBuffer(Device device, Format format)
        : base(device)
    {
        Format = format;
    }

    internal Format Format { get; }

    protected override ID3D11Buffer CreateBuffer(uint sizeInBytes)
    {
        var description = new BufferDescription()
        {
            Usage = ResourceUsage.Dynamic,
            ByteWidth = sizeInBytes,
            BindFlags = BindFlags.IndexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            StructureByteStride = PrimitiveSizeInBytes
        };

        return Device.CreateBuffer(description);
    }
}
