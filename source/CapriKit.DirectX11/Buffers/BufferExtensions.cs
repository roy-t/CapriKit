using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using CapriKit.DirectX11.Resources;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Adds extension methods to device buffers based on how the CPU, GPU and shaders can access the buffers
/// </summary>
public static class BufferExtensions
{
    /// <summary>
    /// Creates a writer that allows you to write data to the buffer.
    /// Unlike Write this method does not ensure the underlying buffer has enough capacity before writing.
    /// </summary>
    public static BufferWriter<T> OpenWriter<T>(this ICpuWriteToBuffer<T> buffer, DeviceContext context)
        where T : unmanaged
    {
        var nativeBuffer = buffer.GetNativeBufferOrThrow();
        return new(context.ID3D11DeviceContext, nativeBuffer);
    }

    /// <summary>
    /// Write the given source data to the underlying device buffer. Use <see cref="OpenWriter{T}(ICpuWriteToBuffer{T}, DeviceContext)"/> when
    /// you plan multiple writes per frame, for improved efficiency.
    /// Unlike OpenWriter this method ensures the underlying buffer has enough capacity before writing.
    /// </summary>
    public static void Write<T>(this ICpuWriteToBuffer<T> buffer, DeviceContext context, ReadOnlySpan<T> source, int offset = 0)
        where T : unmanaged
    {
        buffer.EnsureCapacity(source.Length);
        using var writer = buffer.OpenWriter(context);
        writer.Write(source, 0);
    }

    /// <summary>
    /// Creates a reader that allows you to read data from the buffer.    
    /// </summary>
    public static BufferReader<T> OpenReader<T>(this ICpuReadFromBuffer<T> buffer, DeviceContext context)
        where T : unmanaged
    {
        var nativeBuffer = buffer.GetNativeBufferOrThrow();
        return new BufferReader<T>(context.ID3D11DeviceContext, nativeBuffer);
    }

    /// <summary>
    /// Reads the data from the underlying device buffer. Use <see cref="OpenReader{T}(ICpuReadFromBuffer{T}, DeviceContext)"/> when
    /// you plan multiple reads per frame, for improved efficiency.
    /// </summary>
    public static int Read<T>(ICpuReadFromBuffer<T> buffer, DeviceContext context, Span<T> target, int offset = 0, int length = int.MaxValue)
        where T : unmanaged
    {
        if (buffer.Length < (offset + length))
        {
            throw new InvalidOperationException($"Trying to read {length} elements from offset {offset} while buffer {buffer.Name} only contains {buffer.Length} elements");
        }

        using var reader = buffer.OpenReader(context);
        reader.Read(offset, length, target);
        return length;
    }

    public static IShaderResourceView CreateShaderResourceView<T>(this IShaderReadFromBuffer<T> buffer, HeadlessDevice device)
        where T : unmanaged
    {
        var nativeBuffer = GetNativeBufferOrThrow(buffer);
        var nativeSrv = device.ID3D11Device.CreateShaderResourceView(nativeBuffer, null);
        var srv = new ShaderResourceView(nativeSrv);
        nativeSrv.DebugName = DebugName.For(srv, buffer.Name);

        return srv;
    }

    public static IShaderResourceView CreateShaderResourceView<T>(this IShaderReadFromBuffer<T> buffer, HeadlessDevice device, uint firstElement, uint numElements)
        where T : unmanaged
    {
        var bufferDescription = new BufferShaderResourceView()
        {
            FirstElement = firstElement,
            NumElements = numElements
        };

        var description = new ShaderResourceViewDescription()
        {
            Buffer = bufferDescription,
            Format = Format.Unknown,
            ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Buffer
        };

        var nativeBuffer = GetNativeBufferOrThrow(buffer);
        var nativeSrv = device.ID3D11Device.CreateShaderResourceView(nativeBuffer, description);
        var srv = new ShaderResourceView(nativeSrv);
        nativeSrv.DebugName = DebugName.For(srv, buffer.Name);

        return srv;
    }

    public static IUnorderedAccessView CreateUnorderedAccessView<T>(this IShaderWriteToBuffer<T> buffer, HeadlessDevice device)
        where T : unmanaged
    {
        var nativeBuffer = GetNativeBufferOrThrow(buffer);
        var nativeUav = device.ID3D11Device.CreateUnorderedAccessView(nativeBuffer, null);
        var uav = new UnorderedAccessView(nativeUav);
        nativeUav.DebugName = DebugName.For(uav, buffer.Name);

        return uav;
    }

    public static IUnorderedAccessView CreateUnorderedAccessView<T>(this IShaderWriteToBuffer<T> buffer, HeadlessDevice device, uint firstElement, uint numElements)
    where T : unmanaged
    {
        var bufferDescription = new BufferUnorderedAccessView()
        {
            FirstElement = firstElement,
            NumElements = numElements,
            Flags = BufferUnorderedAccessViewFlags.None
        };

        var description = new UnorderedAccessViewDescription()
        {
            Buffer = bufferDescription,
            Format = Format.Unknown,
            ViewDimension = UnorderedAccessViewDimension.Buffer
        };


        var nativeBuffer = GetNativeBufferOrThrow(buffer);
        var nativeUav = device.ID3D11Device.CreateUnorderedAccessView(nativeBuffer, description);
        var uav = new UnorderedAccessView(nativeUav);
        nativeUav.DebugName = DebugName.For(uav, buffer.Name);

        return uav;
    }

    private static ID3D11Buffer GetNativeBufferOrThrow<T>(this IDeviceBuffer<T> buffer)
        where T : unmanaged
    {
        if (buffer.ID3D11Buffer == null || buffer.Capacity == 0)
        {
            throw new InvalidOperationException($"Cannot read or write from buffer {buffer.Name}. The native buffer is null or has a capacity of 0");
        }

        return buffer.ID3D11Buffer;
    }
}
