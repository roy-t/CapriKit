using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Buffers;

public interface IIndexBuffer<T> : IImmutableDeviceBuffer<T>
    where T : unmanaged
{
    internal Format Format { get; }
}

public static class IndexBuffers
{
    private static readonly BufferDescription MutableBufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.IndexBuffer,
        CPUAccessFlags = CpuAccessFlags.Write,
        MiscFlags = ResourceOptionFlags.None,
    };

    public static IndexBuffer<ushort> CreateU16(Device device, string? hintName = null)
    {
        var description = MutableBufferDescription;
        description.ByteWidth = 0;
        description.StructureByteStride = sizeof(ushort);

        return new IndexBuffer<ushort>(device, description, Format.R16_UInt, hintName);
    }

    public static IndexBuffer<uint> CreateU32(Device device, string? hintName = null)
    {
        var description = MutableBufferDescription;
        description.ByteWidth = 0;
        description.StructureByteStride = sizeof(uint);

        return new IndexBuffer<uint>(device, description, Format.R32_UInt, hintName);
    }

    private static readonly BufferDescription ImmutableBufferDescription = new()
    {
        Usage = ResourceUsage.Immutable,
        BindFlags = BindFlags.IndexBuffer,
        CPUAccessFlags = CpuAccessFlags.None,
        MiscFlags = ResourceOptionFlags.None,
    };

    public static ImmutableIndexBuffer<ushort> CreateU16Immutable(Device device, ReadOnlySpan<ushort> data, string? hintName = null)
    {
        return new ImmutableIndexBuffer<ushort>(device, data, ImmutableBufferDescription, Format.R16_UInt, hintName);
    }

    public static ImmutableIndexBuffer<uint> CreateU32Immutable(Device device, ReadOnlySpan<uint> data, string? hintName = null)
    {
        return new ImmutableIndexBuffer<uint>(device, data, ImmutableBufferDescription, Format.R32_UInt, hintName);
    }
}

/// <summary>
/// Index buffer for the indirect referencing and reusing of vertices.
/// </summary>
public sealed class IndexBuffer<T> : DeviceBuffer<T>, IIndexBuffer<T>, ICpuWriteToBuffer<T>
    where T : unmanaged
{
    private readonly Format InternalFormat;

    internal IndexBuffer(Device device, BufferDescription description, Format format, string? hintName = null)
        : base(device, description)
    {
        InternalFormat = format;
        Name = DebugName.For(this, hintName);
    }

    public override string Name { get; }

    Format IIndexBuffer<T>.Format => InternalFormat;
}

/// <summary>
/// Immutable index buffer for the indirect referencing and reusing of vertices.
/// </summary>
public sealed class ImmutableIndexBuffer<T> : ImmutableDeviceBuffer<T>, IIndexBuffer<T>
    where T : unmanaged
{
    private readonly Format InternalFormat;

    internal ImmutableIndexBuffer(Device device, ReadOnlySpan<T> data, BufferDescription description, Format format, string? hintName = null)
        : base(device, description, data, DebugName.For<ImmutableIndexBuffer<T>>(hintName))
    {
        InternalFormat = format;
    }

    Format IIndexBuffer<T>.Format => InternalFormat;
}
