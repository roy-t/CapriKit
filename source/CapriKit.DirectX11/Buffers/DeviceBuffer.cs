using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public abstract class DeviceBuffer<T> : IDisposable
    where T : unmanaged
{
    protected readonly ID3D11Device Device;

    internal DeviceBuffer(Device device, string nameHint, int capacity)
    {
        Device = device.ID3D11Device;
        unsafe
        {
            PrimitiveSizeInBytes = sizeof(T);
        }
        Name = DebugName.For<DeviceBuffer<T>>(nameHint);

        EnsureCapacity(capacity);
    }

    internal int PrimitiveSizeInBytes { get; }

    public int Capacity { get; private set; }

    public int Length { get; private set; }

    internal ID3D11Buffer Buffer { get; private set; } = null!;

    public string Name { get; }

    public void EnsureCapacity(int primitiveCount, int reserveExtra = 0)
    {
        if (Buffer == null || Capacity < primitiveCount)
        {
            Buffer?.Dispose();
            Capacity = primitiveCount + reserveExtra;
            Length = primitiveCount;

            var bufferSize = Capacity * PrimitiveSizeInBytes;



            Buffer = CreateBuffer(bufferSize);
#if DEBUG
            Buffer.DebugName = Name;
#endif
        }
    }

    /// <summary>
    /// Write the given source data to the underlying device buffer. Use <see cref="OpenWriter(DeviceContext)"/> when
    /// you plan multiple writes per frame, for improved efficiency.
    /// Unlike OpenWriter this method ensures the underlying buffer has enough capacity before writing.
    /// </summary>
    public void Write(DeviceContext context, ReadOnlySpan<T> source, int offset = 0)
    {
        EnsureCapacity(source.Length);
        using var writer = OpenWriter(context);
        writer.Write(source, 0);
    }

    /// <summary>
    /// Reads the data from the underlying device buffer. Use <see cref="OpenReader(DeviceContext)"/> when
    /// you plan multiple reads per frame, for improved efficiency.
    /// This method never reads more data than is available or than fits in target.
    /// </summary>
    public int Read(DeviceContext context, Span<T> target, int offset = 0, int length = int.MaxValue)
    {
        using var reader = OpenReader(context);
        length = Math.Min(Length, Math.Min(target.Length, length));
        reader.Read(offset, length, target);

        return length;
    }

    public BufferWriter<T> OpenWriter(DeviceContext context)
    {
        return new(context.ID3D11DeviceContext, Buffer);
    }

    public BufferReader<T> OpenReader(DeviceContext context)
    {
        return new BufferReader<T>(context.ID3D11DeviceContext, Buffer);
    }

    public virtual void Dispose()
    {
        Buffer?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected abstract ID3D11Buffer CreateBuffer(int sizeInBytes);
}
