using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Holds vertex data to render in a shader
/// </summary>
public sealed class VertexBuffer<T> : DeviceBuffer<T>, ICpuWriteToBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.VertexBuffer,
        CPUAccessFlags = CpuAccessFlags.Write,
    };

    public VertexBuffer(HeadlessDevice device, string? nameHint = null)
        : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}
