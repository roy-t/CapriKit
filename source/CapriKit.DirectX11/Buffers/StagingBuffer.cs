using CapriKit.DirectX11.Contexts;
using CapriKit.DirectX11.Debug;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Used to read back data from GPU buffers by first copying them to a buffer that is CPU readable.
/// TODO: Figure out what we need to do to also read back texture data. Can we just read that directy if mapped?
/// </summary>
public sealed class StagingBuffer<T> : DeviceBuffer<T>, ICpuReadFromBuffer<T>
    where T : unmanaged

{
    private static readonly BufferDescription BufferDescription = new()
    {
        Usage = ResourceUsage.Staging,
        BindFlags = BindFlags.None,
        CPUAccessFlags = CpuAccessFlags.Read,
        MiscFlags = ResourceOptionFlags.None,
    };

    public StagingBuffer(HeadlessDevice device, string? nameHint = null) : base(device, BufferDescription)
    {
        Name = DebugName.For(this, nameHint);
    }

    /// <summary>
    /// Copies the data of the given GPU buffer to the staging buffer, so that it can be read by the CPU.    
    /// </summary>    
    public void CopyResourceToStagingBuffer(DeviceContext context, DeviceBuffer<T> source)
    {
        EnsureCapacity(source.Length);
        context.ID3D11DeviceContext.CopyResource(nativeBuffer, source.nativeBuffer);
    }

    public override string Name { get; }
}
