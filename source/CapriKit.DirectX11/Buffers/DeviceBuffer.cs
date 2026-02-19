using CapriKit.DirectX11.Contexts;
using System.Diagnostics.CodeAnalysis;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public abstract class DeviceBuffer<T> : IDisposable
    where T : unmanaged
{
    protected readonly ID3D11Device Device;

    internal DeviceBuffer(Device device)
    {
        Device = device.ID3D11Device;
        unsafe
        {
            PrimitiveSizeInBytes = (uint)sizeof(T);
        }
    }

    internal uint PrimitiveSizeInBytes { get; }

    public int Capacity { get; private set; }

    public int Length { get; private set; }

    internal ID3D11Buffer? Buffer { get; private set; }

    public abstract string Name { get; }

    [MemberNotNull(nameof(Buffer))]
    public void EnsureCapacity(int primitiveCount, int reserveExtra = 0)
    {
        if (Buffer == null || Capacity < primitiveCount)
        {
            Buffer?.Dispose();
            Capacity = primitiveCount + reserveExtra;
            Length = (int)primitiveCount;

            var bufferSize = Capacity * PrimitiveSizeInBytes;

            Buffer = CreateBuffer((uint)bufferSize);
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
        length = Math.Min(Length, Math.Min(target.Length, length));
        if (Buffer == null || length == 0) { return 0; }

        using var reader = OpenReader(context);
        reader.Read(offset, length, target);
        return length;
    }

    public BufferWriter<T> OpenWriter(DeviceContext context)
    {
        ThrowOnUnallocatedBuffer();
        return new(context.ID3D11DeviceContext, Buffer);
    }

    public BufferReader<T> OpenReader(DeviceContext context)
    {
        ThrowOnUnallocatedBuffer();
        return new BufferReader<T>(context.ID3D11DeviceContext, Buffer);
    }

    public virtual void Dispose()
    {
        Buffer?.Dispose();
        GC.SuppressFinalize(this);
    }

    [MemberNotNull(nameof(Buffer))]
    private void ThrowOnUnallocatedBuffer()
    {
        if (Buffer == null || Capacity == 0)
        {
            throw new InvalidOperationException("Cannot read or write from a buffer that is null or has a capacity of 0");
        }
    }

    protected abstract ID3D11Buffer CreateBuffer(uint sizeInBytes);
}
