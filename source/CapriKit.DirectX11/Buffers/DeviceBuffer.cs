using System.Diagnostics.CodeAnalysis;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public abstract class DeviceBuffer<T> : IDeviceBuffer<T>, IDisposable
    where T : unmanaged
{
    private readonly BufferDescription BufferDescription;

    internal readonly ID3D11Device Device;
    internal readonly uint PrimitiveSizeInBytes;
    internal ID3D11Buffer? nativeBuffer;

    internal DeviceBuffer(HeadlessDevice device, BufferDescription description)
    {
        Device = device.ID3D11Device;
        BufferDescription = description;
        unsafe
        {
            PrimitiveSizeInBytes = (uint)sizeof(T);
        }
    }

    public int Capacity { get; private set; }

    public int Length { get; private set; }

    public abstract string Name { get; }

    ID3D11Buffer? IDeviceBuffer<T>.ID3D11Buffer => nativeBuffer;

    [MemberNotNull(nameof(nativeBuffer))]
    public void EnsureCapacity(int primitiveCount, int reserveExtra = 0)
    {
        if (nativeBuffer == null || Capacity < primitiveCount)
        {
            nativeBuffer?.Dispose();
            Capacity = primitiveCount + reserveExtra;
            Length = (int)primitiveCount;

            var bufferSize = Capacity * PrimitiveSizeInBytes;

            nativeBuffer = CreateBuffer((uint)bufferSize);
#if DEBUG
            nativeBuffer.DebugName = Name;
#endif
        }
    }

    private ID3D11Buffer CreateBuffer(uint sizeInBytes)
    {
        var resizedBufferDescription = BufferDescription;
        resizedBufferDescription.ByteWidth = sizeInBytes;
        resizedBufferDescription.StructureByteStride = PrimitiveSizeInBytes;

        return Device.CreateBuffer(resizedBufferDescription);
    }

    public virtual void Dispose()
    {
        nativeBuffer?.Dispose();
        GC.SuppressFinalize(this);
    }
}
