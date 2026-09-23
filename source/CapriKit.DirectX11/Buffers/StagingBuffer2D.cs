using CapriKit.DirectX11.Debug;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace CapriKit.DirectX11.Buffers;

public sealed class StagingBuffer2D<T> : IDisposable
    where T : unmanaged
{
    private readonly ID3D11Texture2D Buffer;

    internal StagingBuffer2D(Device device, Texture2DDescription description, string? nameHint = null)
    {
        description.BindFlags = BindFlags.None;
        description.CPUAccessFlags = CpuAccessFlags.Read;
        description.Usage = ResourceUsage.Staging;
        Buffer = device.ID3D11Device.CreateTexture2D(description);
#if DEBUG
        Buffer.DebugName = DebugName.For(this, nameHint);
#endif
    }

    internal unsafe T[] CopySurfaceDataToSpan(ID3D11DeviceContext context, ID3D11Texture2D source, uint mipSlice = 0, uint arraySlice = 0)
    {
        // Since textures can have padding, based on driver alignment rules, we need to copy row-by-row.
        context.CopyResource(Buffer, source);
        var subResource = Buffer.CalculateSubResourceIndex(mipSlice, arraySlice, out var rows);
        var mapped = context.Map(Buffer, subResource, MapMode.Read, MapFlags.None);
        try
        {
            var primitiveSizeInBytes = sizeof(T);
            var width = Buffer.Description.GetWidth(mipSlice);
            var height = Buffer.Description.GetHeight(mipSlice);
            var output = new T[width * height];

            for (var row = 0u; row < rows; row++)
            {
                var rowSpan = new ReadOnlySpan<byte>((byte*)mapped.DataPointer + row * mapped.RowPitch, (int)width * primitiveSizeInBytes);
                var typedRowSpan = MemoryMarshal.Cast<byte, T>(rowSpan);
                var targetSpan = output.AsSpan((int)(row * width));
                typedRowSpan.CopyTo(targetSpan);
            }

            return output;
        }
        finally
        {
            context.Unmap(Buffer, mipSlice, arraySlice);
        }
    }

    public void Dispose()
    {
        Buffer.Dispose();
    }
}
