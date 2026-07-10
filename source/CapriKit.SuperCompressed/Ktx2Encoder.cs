using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// Encodes images into supercompressed KTX2 textures, which <see cref="Ktx2Transcoder"/>
/// can later transcode to GPU-native formats.
/// </summary>
public static class Ktx2Encoder
{
    public const int MinQuality = 1;
    public const int MaxQuality = 100;
    public const int MinEffort = 0;
    public const int MaxEffort = 10;

    /// <summary>Encodes an image into a KTX2 texture.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="format">The intermediate texture format stored inside the KTX2 file.</param>
    /// <param name="quality">Encoding quality, <see cref="MinQuality"/> (smallest) to <see cref="MaxQuality"/> (best).</param>
    /// <param name="effort">Encoding effort, <see cref="MinEffort"/> (fastest) to <see cref="MaxEffort"/> (best).</param>
    /// <param name="flags">
    /// Encoding options, for example <see cref="CompressionFlags.Srgb"/> for color textures,
    /// <see cref="CompressionFlags.GenMipsClamp"/> or <see cref="CompressionFlags.GenMipsWrap"/> to
    /// generate mipmaps, and <see cref="CompressionFlags.Threaded"/> to use all cores.
    /// </param>
    /// <returns>The contents of a .ktx2 file.</returns>
    public static byte[] Encode(Image image, BasisTexFormat format, int quality = 85, int effort = 2, CompressionFlags flags = CompressionFlags.None)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(quality, MinQuality);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, MaxQuality);
        ArgumentOutOfRangeException.ThrowIfLessThan(effort, MinEffort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(effort, MaxEffort);

        using var compParams = NativeMethods.bu_new_comp_params();
        if (compParams.IsInvalid)
        {
            throw new InvalidOperationException("Failed to create basis_universal compression parameters");
        }

        var width = (uint)image.Width;
        var height = (uint)image.Height;
        if (!NativeMethods.bu_comp_params_set_image_rgba32(compParams, 0, image.Pixels, width, height, width * 4))
        {
            throw new InvalidOperationException("basis_universal rejected the source image");
        }

        if (!NativeMethods.bu_compress_texture(compParams, format, quality, effort, flags | CompressionFlags.Ktx2Output, 0.0f))
        {
            throw new InvalidOperationException($"basis_universal failed to encode the image to {format}");
        }

        var size = checked((int)NativeMethods.bu_comp_params_get_comp_data_size(compParams));
        var pCompressedData = NativeMethods.bu_comp_params_get_comp_data_ofs(compParams);

        var ktx2 = new byte[size];
        Marshal.Copy(pCompressedData, ktx2, 0, size);
        return ktx2;
    }
}
