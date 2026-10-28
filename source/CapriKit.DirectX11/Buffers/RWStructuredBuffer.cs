using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// A specific structured buffer that shaders can read and write to. See <seealso cref="StagingBuffer{T}"/>. For
/// transferring the data back to the CPU.
/// </summary>
public sealed class RWStructuredBuffer<T> : DeviceBuffer<T>, IShaderReadFromBuffer<T>, IShaderWriteToBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Default,
        BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        CPUAccessFlags = CpuAccessFlags.None,
        MiscFlags = ResourceOptionFlags.BufferStructured,
    };

    public RWStructuredBuffer(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, BufferDescription)
    {
        Name = DebugName.For(this, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    public override string Name { get; }
}
