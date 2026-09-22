using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// A buffer that the is instantiated with the given data and can then
/// no longer be accessed by the CPU or changed by the GPU.
/// </summary>
public abstract class ImmutableDeviceBuffer<T> : IImmutableDeviceBuffer<T>, IDisposable
    where T : unmanaged
{
    internal ID3D11Buffer nativeBuffer;

    internal ImmutableDeviceBuffer(Device device, BufferDescription description, ReadOnlySpan<T> data, string name)
    {
        if (data.Length < 1)
        {
            throw new ArgumentException("Span is empty", nameof(data));
        }

        Length = data.Length;
        Name = name;
        unsafe
        {
            PrimitiveSizeInBytes = (uint)sizeof(T);
        }
        description.ByteWidth = ((uint)data.Length) * PrimitiveSizeInBytes;
        description.StructureByteStride = PrimitiveSizeInBytes;
        nativeBuffer = device.ID3D11Device.CreateBuffer(data, description);
#if DEBUG
        nativeBuffer.DebugName = name;
#endif
    }

    public uint PrimitiveSizeInBytes { get; }

    public string Name { get; }
    public int Length { get; }
    ID3D11Buffer? IImmutableDeviceBuffer<T>.ID3D11Buffer => nativeBuffer;

    public virtual void Dispose()
    {
        nativeBuffer.Dispose();
        GC.SuppressFinalize(this);
    }
}
