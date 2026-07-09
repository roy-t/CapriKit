using StbImageSharp;

namespace CapriKit.SuperCompressed;

/// <summary>
/// An uncompressed 32 bits-per-pixel RGBA image, the input format of <see cref="Ktx2Encoder"/>.
/// </summary>
public sealed class Image
{
    private readonly byte[] pixels;

    /// <summary>
    /// Loads a JPG, PNG, BMP, TGA, PSD or GIF image using StbImageSharp,
    /// converting it to RGBA32 when necessary.
    /// </summary>
    public static Image Load(Stream stream)
    {
        var result = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new Image(result.Data, result.Width, result.Height);
    }

    /// <inheritdoc cref="Load(Stream)"/>
    public static Image Load(byte[] fileData)
    {
        var result = ImageResult.FromMemory(fileData, ColorComponents.RedGreenBlueAlpha);
        return new Image(result.Data, result.Width, result.Height);
    }

    /// <summary>
    /// Wraps existing RGBA32 pixel data in an image. The array is not copied:
    /// later changes to it are visible through <see cref="Pixels"/>.
    /// </summary>
    public Image(byte[] rgba32Pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(rgba32Pixels);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        if (rgba32Pixels.Length != (long)width * height * 4)
        {
            throw new ArgumentException($"Expected {width}x{height}x4 = {(long)width * height * 4} bytes of RGBA32 pixel data, got {rgba32Pixels.Length} bytes", nameof(rgba32Pixels));
        }

        pixels = rgba32Pixels;
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>The pixel data in row-major RGBA order, 4 bytes per pixel, no padding between rows.</summary>
    public ReadOnlySpan<byte> Pixels => pixels;
}
