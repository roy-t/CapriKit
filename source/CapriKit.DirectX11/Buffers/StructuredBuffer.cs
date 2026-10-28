using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Send structs to the GPU so the shader can read them.
/// </summary>
public sealed class StructuredBuffer<T> : DeviceBuffer<T>, ICpuWriteToBuffer<T>, IShaderReadFromBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.ShaderResource,
        CPUAccessFlags = CpuAccessFlags.Write,
        MiscFlags = ResourceOptionFlags.BufferStructured,
    };

    public StructuredBuffer(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, BufferDescription)
    {
        Name = DebugName.For(this, caller: caller, callerFile: callerFile, callerLine: callerLine);
    }

    public override string Name { get; }
}
