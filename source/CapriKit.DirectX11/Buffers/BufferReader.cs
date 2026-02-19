using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

/// <summary>
/// Allows the CPU to read GPU data
/// </summary>
public readonly ref struct BufferReader<T> : IDisposable
    where T : unmanaged
{
    private readonly ID3D11DeviceContext Context;
    private readonly ID3D11Buffer Source;
    private readonly MappedSubresource Resource;

    internal BufferReader(ID3D11DeviceContext context, ID3D11Buffer source)
    {
        Context = context;
        Source = source;

        Resource = context.Map(source, 0, MapMode.Read, MapFlags.None);
        context.Flush();
    }

    public void Read(int offset, int length, Span<T> target)
    {
        var span = Resource.AsSpan<T>(Source);
        var slice = span.Slice(offset, length);
        slice.CopyTo(target);
    }

    public void Dispose()
    {
        Context.Unmap(Source);
    }
}
