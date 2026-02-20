using Vortice.DXGI;

namespace CapriKit.DirectX11;

internal static class FormatExtensions
{
    public static uint GetBytesPerPixel(this Format format)
    {
        return format.GetBitsPerPixel() / 8;
    }
}
