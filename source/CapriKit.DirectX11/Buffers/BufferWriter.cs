using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public readonly ref struct BufferWriter<T> : IDisposable
    where T : unmanaged
{
    private readonly ID3D11DeviceContext Context;
    private readonly ID3D11Buffer Buffer;
    private readonly MappedSubresource Resource;

    internal BufferWriter(ID3D11DeviceContext context, ID3D11Buffer buffer)
    {
        Context = context;
        Buffer = buffer;
        Resource = context.Map(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
    }

    public void Write(ReadOnlySpan<T> source, int offset)
    {
        var span = Resource.AsSpan<T>(Buffer);
        var slice = span.Slice(offset, source.Length);
        source.CopyTo(slice);
    }

    public void Dispose()
    {
        Context.Unmap(Buffer);
    }
}
