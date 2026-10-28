using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;


/// <summary>
/// Holds vertex data to render in a shader
/// </summary>
public interface IVertexBuffer<T> : IImmutableDeviceBuffer<T>
    where T : unmanaged
{
}

/// <summary>
/// Holds vertex data to render in a shader
/// </summary>
public sealed class VertexBuffer<T> : DeviceBuffer<T>, ICpuWriteToBuffer<T>, IVertexBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.VertexBuffer,
        CPUAccessFlags = CpuAccessFlags.Write,
    };

    public VertexBuffer(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, BufferDescription)
    {
        Name = DebugName.For(this, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    public override string Name { get; }
}

/// <summary>
/// Immutable buffer that holds vertex data to render in a shader
/// </summary>
public sealed class ImmutableVertexBuffer<T> : ImmutableDeviceBuffer<T>, IVertexBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Immutable,
        BindFlags = BindFlags.VertexBuffer,
        CPUAccessFlags = CpuAccessFlags.None,
        MiscFlags = ResourceOptionFlags.None,
    };

    public ImmutableVertexBuffer(Device device, ReadOnlySpan<T> data, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, BufferDescription, data, DebugName.For<ImmutableVertexBuffer<T>>(caller: caller, callerFile: callerFile, callerLine: callerLine)) { }

}
