using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
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

    public static IndexBuffer<ushort> CreateU16(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var name = DebugName.For<IndexBuffer<ushort>>(caller: caller, callerFile: callerFile, callerLine: callerLine);
        return new IndexBuffer<ushort>(device, MutableBufferDescription, Format.R16_UInt, name);
    }

    public static IndexBuffer<uint> CreateU32(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var name = DebugName.For<IndexBuffer<uint>>(caller: caller, callerFile: callerFile, callerLine: callerLine);
        return new IndexBuffer<uint>(device, MutableBufferDescription, Format.R32_UInt, name);
    }

    private static readonly BufferDescription ImmutableBufferDescription = new()
    {
        Usage = ResourceUsage.Immutable,
        BindFlags = BindFlags.IndexBuffer,
        CPUAccessFlags = CpuAccessFlags.None,
        MiscFlags = ResourceOptionFlags.None,
    };

    public static ImmutableIndexBuffer<ushort> CreateU16Immutable(Device device, ReadOnlySpan<ushort> data, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var name = DebugName.For<ImmutableIndexBuffer<ushort>>(caller: caller, callerFile: callerFile, callerLine: callerLine);
        return new ImmutableIndexBuffer<ushort>(device, data, ImmutableBufferDescription, Format.R16_UInt, name);
    }

    public static ImmutableIndexBuffer<uint> CreateU32Immutable(Device device, ReadOnlySpan<uint> data, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
    {
        var name = DebugName.For<ImmutableIndexBuffer<uint>>(caller: caller, callerFile: callerFile, callerLine: callerLine);
        return new ImmutableIndexBuffer<uint>(device, data, ImmutableBufferDescription, Format.R32_UInt, name);
    }
}

/// <summary>
/// Index buffer for the indirect referencing and reusing of vertices.
/// </summary>
public sealed class IndexBuffer<T> : DeviceBuffer<T>, IIndexBuffer<T>, ICpuWriteToBuffer<T>
    where T : unmanaged
{
    private readonly Format InternalFormat;

    internal IndexBuffer(Device device, BufferDescription description, Format format, string name)
        : base(device, description)
    {
        InternalFormat = format;
        Name = name;
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

    internal ImmutableIndexBuffer(Device device, ReadOnlySpan<T> data, BufferDescription description, Format format, string name)
        : base(device, description, data, name)
    {
        InternalFormat = format;
    }

    Format IIndexBuffer<T>.Format => InternalFormat;
}
