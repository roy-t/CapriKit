using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// An immutable structured buffer that the GPU can read from.
/// </summary>
public sealed class ImmutableStructuredBuffer<T> : ImmutableDeviceBuffer<T>, IShaderReadFromBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription Description = new()
    {
        Usage = ResourceUsage.Immutable,
        BindFlags = BindFlags.ShaderResource,
        CPUAccessFlags = CpuAccessFlags.None,
        MiscFlags = ResourceOptionFlags.BufferStructured,
    };

    public ImmutableStructuredBuffer(Device device, ReadOnlySpan<T> data, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, Description, data, DebugName.For<ImmutableStructuredBuffer<T>>(caller: caller, callerFile: callerFile, callerLine: callerLine)) { }

}
