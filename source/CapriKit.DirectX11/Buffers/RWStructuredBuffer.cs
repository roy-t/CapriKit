using CapriKit.DirectX11.Debug;
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

    public RWStructuredBuffer(Device device, string? nameHint = null) : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}
