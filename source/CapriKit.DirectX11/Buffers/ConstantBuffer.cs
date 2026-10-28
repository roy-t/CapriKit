using CapriKit.DirectX11.Debug;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Constant buffers are buffers optimized to let the CPU write a small piece of information to the GPU every frame.
/// For example, the camera position. For more flexible use see <seealso cref="StructuredBuffer{T}"/>.
/// Note: The structures used in constant buffers must match the packing rules described in
/// https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-packing-rules
/// </summary>
public sealed class ConstantBuffer<T> : DeviceBuffer<T>, ICpuWriteToBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.ConstantBuffer,
        CPUAccessFlags = CpuAccessFlags.Write,
    };

    public ConstantBuffer(Device device, [CallerMemberName] string? caller = null, [CallerFilePath] string? callerFile = null, [CallerLineNumber] int callerLine = 0)
        : base(device, BufferDescription)
    {
        Name = DebugName.For(this, caller: caller, callerFile: callerFile, callerLine: callerLine);
        EnsureCapacity(1);
    }

    public override string Name { get; }
}
