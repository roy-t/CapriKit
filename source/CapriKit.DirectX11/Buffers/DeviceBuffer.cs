using System.Diagnostics.CodeAnalysis;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public abstract class DeviceBuffer<T> : IDeviceBuffer<T>, IDisposable
    where T : unmanaged
{
    private readonly BufferDescription BufferDescription;

    internal readonly ID3D11Device Device;
    internal ID3D11Buffer? nativeBuffer;

    internal DeviceBuffer(Device device, BufferDescription description)
    {
        Device = device.ID3D11Device;
        BufferDescription = description;
        unsafe
        {
            PrimitiveSizeInBytes = (uint)sizeof(T);
        }
    }

    public uint PrimitiveSizeInBytes { get; }

    public int Capacity { get; private set; }

    public int Length { get; private set; }

    public abstract string Name { get; }

    ID3D11Buffer? IImmutableDeviceBuffer<T>.ID3D11Buffer => nativeBuffer;

    /// <summary>
    /// Grows or shrinks the capacity of the buffer to the exact primitive count, discarding all existing data.
    /// If the buffer was already the right capacity, nothing happens.
    /// </summary>
    [MemberNotNull(nameof(nativeBuffer))]
    public void SetCapacity(int primitiveCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primitiveCount, 1, nameof(primitiveCount));

        if (nativeBuffer == null || Capacity != primitiveCount)
        {
            RecreateBuffer(primitiveCount);
        }
        Length = primitiveCount;
    }

    /// <summary>
    /// Grows the capacity of the buffer to primitiveCount+reserveExtra if the current capacity is less then primitiveCount,
    /// discarding all existing data.
    /// If the buffer could already fit at least primitiveCount items, nothing happens.
    /// </summary>
    [MemberNotNull(nameof(nativeBuffer))]
    public void EnsureCapacity(int primitiveCount, int reserveExtra = 0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(primitiveCount, 1, nameof(primitiveCount));
        ArgumentOutOfRangeException.ThrowIfNegative(reserveExtra, nameof(reserveExtra));

        if (nativeBuffer == null || Capacity < primitiveCount)
        {
            RecreateBuffer(primitiveCount + reserveExtra);
        }
        Length = primitiveCount;
    }

    [MemberNotNull(nameof(nativeBuffer))]
    private void RecreateBuffer(int capacity)
    {
        var resizedBufferDescription = BufferDescription;
        resizedBufferDescription.ByteWidth = PrimitiveSizeInBytes * (uint)capacity;
        resizedBufferDescription.StructureByteStride = PrimitiveSizeInBytes;

        var buffer = Device.CreateBuffer(resizedBufferDescription);
        nativeBuffer?.Dispose();
        nativeBuffer = buffer;
        Capacity = capacity;
#if DEBUG
        nativeBuffer.DebugName = Name;
#endif
    }

    public virtual void Dispose()
    {
        nativeBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
