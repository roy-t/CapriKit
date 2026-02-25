using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// A specific structured buffer that the shader can also write to, so the CPU can read back the data. To read back data from other buffers use a
/// <seealso cref="StagingBuffer{T}"/>.
/// </summary>
public sealed class RWStructuredBuffer<T> : DeviceBuffer<T>, ICpuReadFromBuffer<T>, ICpuWriteToBuffer<T>, IShaderReadFromBuffer<T>, IShaderWriteToBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Default,
        BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        CPUAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
        MiscFlags = ResourceOptionFlags.BufferStructured,
    };

    public RWStructuredBuffer(Device device, string? nameHint = null) : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}
