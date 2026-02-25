using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Send structs to the GPU so the shader can read them
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

    public StructuredBuffer(Device device, string? nameHint = null) : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}
