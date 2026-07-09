using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// Reads KTX2 textures (as produced by <see cref="Encoder"/>) and transcodes their mip
/// levels to GPU-native formats. Open a texture with <see cref="Open"/>, then pass the
/// returned handle to the other methods.
/// </summary>
/// <remarks>
/// Do not call <see cref="Transcode"/> concurrently with the same <see cref="Ktx2FileHandle"/>.
/// (The native transcoder would need a per-thread transcode state for that, which this
/// wrapper does not use.)
/// </remarks>
public static class Ktx2Transcoder
{
    /// <summary>Parses the contents of a .ktx2 file and prepares it for transcoding.</summary>
    /// <exception cref="InvalidDataException">The data is not a supported KTX2 texture.</exception>
    public static unsafe Ktx2FileHandle Open(ReadOnlySpan<byte> ktx2Data)
    {
        // The native KTX2 reader references the buffer for the lifetime of the handle, so
        // it is copied to the pinned object heap, where it never moves, and stored in the
        // handle, which keeps it reachable while the native side can still read it.
        var pinnedData = GC.AllocateUninitializedArray<byte>(ktx2Data.Length, pinned: true);
        ktx2Data.CopyTo(pinnedData);

        IntPtr rawHandle;
        fixed (byte* pData = pinnedData)
        {
            rawHandle = NativeMethods.bt_ktx2_open((IntPtr)pData, (uint)pinnedData.Length);
        }

        if (rawHandle == IntPtr.Zero)
        {
            throw new InvalidDataException("Not a valid KTX2 file");
        }

        var ktx2File = new Ktx2FileHandle(pinnedData);
        Marshal.InitHandle(ktx2File, rawHandle);

        if (!NativeMethods.bt_ktx2_start_transcoding(ktx2File))
        {
            ktx2File.Dispose();
            throw new InvalidDataException("Failed to prepare the KTX2 file for transcoding");
        }

        return ktx2File;
    }

    /// <summary>Width in pixels of the largest mip level.</summary>
    public static int GetWidth(Ktx2FileHandle ktx2File)
    {
        return (int)NativeMethods.bt_ktx2_get_width(ktx2File);
    }

    /// <summary>Height in pixels of the largest mip level.</summary>
    public static int GetHeight(Ktx2FileHandle ktx2File)
    {
        return (int)NativeMethods.bt_ktx2_get_height(ktx2File);
    }

    /// <summary>The number of mip levels.</summary>
    public static int GetLevels(Ktx2FileHandle ktx2File)
    {
        return (int)NativeMethods.bt_ktx2_get_levels(ktx2File);
    }

    /// <summary>The number of array layers; 0 for textures that are not arrays.</summary>
    public static int GetLayers(Ktx2FileHandle ktx2File)
    {
        return (int)NativeMethods.bt_ktx2_get_layers(ktx2File);
    }

    /// <summary>The number of faces: 6 for cubemaps, 1 otherwise.</summary>
    public static int GetFaces(Ktx2FileHandle ktx2File)
    {
        return (int)NativeMethods.bt_ktx2_get_faces(ktx2File);
    }

    /// <summary>The intermediate texture format stored inside the KTX2 file.</summary>
    public static BasisTexFormat GetFormat(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_basis_tex_format(ktx2File);
    }

    public static bool HasAlpha(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_has_alpha(ktx2File);
    }

    public static bool IsSrgb(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_is_srgb(ktx2File);
    }

    /// <summary>Transcodes one mip level to the given GPU texture format.</summary>
    public static TranscodedImage Transcode(Ktx2FileHandle ktx2File, TranscodeFormat format, int level = 0, int layer = 0, int face = 0, DecodeFlags flags = DecodeFlags.None)
    {
        var width = NativeMethods.bt_ktx2_get_level_orig_width(ktx2File, (uint)level, (uint)layer, (uint)face);
        var height = NativeMethods.bt_ktx2_get_level_orig_height(ktx2File, (uint)level, (uint)layer, (uint)face);
        if (width == 0 || height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level), $"The KTX2 file does not contain level {level}, layer {layer}, face {face}");
        }

        // For compressed formats sizes are expressed in blocks, for uncompressed formats in pixels
        var bytesPerUnit = NativeMethods.bt_basis_get_bytes_per_block_or_pixel(format);
        uint rowPitch;
        if (NativeMethods.bt_basis_transcoder_format_is_uncompressed(format))
        {
            rowPitch = width * bytesPerUnit;
        }
        else
        {
            var blockWidth = NativeMethods.bt_basis_get_block_width(format);
            var blocksPerRow = (width + blockWidth - 1) / blockWidth;
            rowPitch = blocksPerRow * bytesPerUnit;
        }

        var sizeInBytes = NativeMethods.bt_basis_compute_transcoded_image_size_in_bytes(format, width, height);
        var data = new byte[sizeInBytes];
        var succeeded = NativeMethods.bt_ktx2_transcode_image_level(
            ktx2File,
            (uint)level, (uint)layer, (uint)face,
            data,
            sizeInBytes / bytesPerUnit,
            format,
            flags,
            outputRowPitchInBlocksOrPixels: 0,
            outputRowsInPixels: 0,
            channel0: -1, channel1: -1,
            stateHandle: 0);

        if (!succeeded)
        {
            throw new InvalidOperationException($"Failed to transcode level {level}, layer {layer}, face {face} from {GetFormat(ktx2File)} to {format}");
        }

        return new TranscodedImage(format, (int)width, (int)height, (int)rowPitch, data);
    }
}
