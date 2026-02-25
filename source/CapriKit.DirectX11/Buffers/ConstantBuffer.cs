using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Constant buffers are buffers optimized to let the CPU write a small piece of information to the GPU every frame.
/// For example, the camera position. For more flexible use see <seealso cref="StructuredBuffer{T}"/>.
/// Note: The structures used in constant buffers must match the packing described here:
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

    public ConstantBuffer(HeadlessDevice device, string? nameHint = null) : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
        EnsureCapacity(1);
    }

    public override string Name { get; }
}
