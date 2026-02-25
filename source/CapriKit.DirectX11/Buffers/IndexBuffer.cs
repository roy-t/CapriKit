using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace CapriKit.DirectX11.Buffers;

/// <inheritdoc/>
public sealed class IndexBufferU16 : IndexBuffer<ushort>
{
    public IndexBufferU16(HeadlessDevice device, string? nameHint = null) : base(device, Format.R16_UInt)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}

/// <inheritdoc/>
public sealed class IndexBufferU32 : IndexBuffer<uint>
{
    public IndexBufferU32(HeadlessDevice device, string? nameHint = null) : base(device, Format.R32_UInt)
    {
        Name = DebugName.For(this, nameHint);
    }

    public override string Name { get; }
}

/// <summary>
/// Allows for indexed drawing techniques. The GPU reads the indices one by one, each index refers to a specific vertex in the vertex buffer.
/// Vertices are usually much larger than a single int or short and are connected to multiple triangles. Using an index buffer means we have
/// to use less GPU memory and have better data locality.
/// </summary>
public abstract class IndexBuffer<T> : DeviceBuffer<T>, ICpuWriteToBuffer<T>
    where T : unmanaged
{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Dynamic,
        BindFlags = BindFlags.IndexBuffer,
        CPUAccessFlags = CpuAccessFlags.Write,
    };


    internal IndexBuffer(HeadlessDevice device, Format format)
        : base(device, BufferDescription)
    {
        Format = format;
    }

    internal Format Format { get; }
}
