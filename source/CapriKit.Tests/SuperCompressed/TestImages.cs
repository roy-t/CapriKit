using CapriKit.SuperCompressed;

namespace CapriKit.Tests.SuperCompressed;

internal static class TestImages
{
    /// <summary>Creates an opaque RGBA32 image with a red/green gradient.</summary>
    public static Image CreateGradient(int width = 16, int height = 16)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i + 0] = (byte)(x * 255 / (width - 1));
                pixels[i + 1] = (byte)(y * 255 / (height - 1));
                pixels[i + 2] = 128;
                pixels[i + 3] = 255;
            }
        }

        return new Image(pixels, width, height);
    }

    /// <summary>
    /// Creates the contents of a 32-bit uncompressed true-color TGA file
    /// with a single red pixel, followed by green, blue and white pixels.
    /// </summary>
    public static byte[] CreateTgaFile()
    {
        var tga = new byte[18 + 4 * 4];
        tga[2] = 2; // Uncompressed true-color
        tga[12] = 2; // Width: 2
        tga[14] = 2; // Height: 2
        tga[16] = 32; // Bits per pixel
        tga[17] = 0x28; // 8 alpha bits, top-left origin

        // Pixels are stored as BGRA
        ReadOnlySpan<byte> pixels =
        [
            0, 0, 255, 255, // Red
            0, 255, 0, 255, // Green
            255, 0, 0, 255, // Blue
            255, 255, 255, 255, // White
        ];
        pixels.CopyTo(tga.AsSpan(18));
        return tga;
    }
}
