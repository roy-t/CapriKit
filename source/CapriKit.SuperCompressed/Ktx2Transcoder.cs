using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// Reads KTX2 textures (as produced by <see cref="Ktx2Encoder"/>) and transcodes their mip
/// levels to GPU-native formats. Open a texture with <see cref="Open"/>, then pass the
/// returned handle to the other methods.
/// </summary>
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
    public static uint GetWidth(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_width(ktx2File);
    }

    /// <summary>Height in pixels of the largest mip level.</summary>
    public static uint GetHeight(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_height(ktx2File);
    }

    /// <summary>The number of mip levels.</summary>
    public static uint GetLevels(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_levels(ktx2File);
    }

    /// <summary>The number of array layers; 0 for textures that are not arrays.</summary>
    public static uint GetLayers(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_layers(ktx2File);
    }

    /// <summary>The number of faces: 6 for cubemaps, 1 otherwise.</summary>
    public static uint GetFaces(Ktx2FileHandle ktx2File)
    {
        return NativeMethods.bt_ktx2_get_faces(ktx2File);
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
    /// <remarks>
    /// Do not use concurrently with the same <see cref="Ktx2FileHandle"/>: it uses the file's
    /// shared transcode state. Use <see cref="TranscodeAll"/> to process a file on all cores.
    /// </remarks>
    public static TranscodedImage Transcode(Ktx2FileHandle ktx2File, TranscodeFormat format, uint level = 0, uint layer = 0, uint face = 0, DecodeFlags flags = DecodeFlags.None)
    {
        return Transcode(ktx2File, format, level, layer, face, flags, stateHandle: 0);
    }

    /// <summary>
    /// Transcodes every mip level, layer, and face in the file to the given GPU texture
    /// format, in parallel. The result is ordered by level, then layer, then face.
    /// </summary>
    public static TranscodedImage[] TranscodeAll(Ktx2FileHandle ktx2File, TranscodeFormat format, DecodeFlags flags = DecodeFlags.None)
    {
        var levels = GetLevels(ktx2File);
        var layers = Math.Max(GetLayers(ktx2File), 1); // 0 for textures that are not arrays
        var faces = GetFaces(ktx2File);

        var work = new List<(uint Level, uint Layer, uint Face)>();
        for (var level = 0u; level < levels; level++)
        {
            for (var layer = 0u; layer < layers; layer++)
            {
                for (var face = 0u; face < faces; face++)
                {
                    work.Add((level, layer, face));
                }
            }
        }

        // Transcoding the same file from multiple threads is only safe when every thread
        // uses its own native transcode state: each worker creates one before its first
        // item and destroys it after its last, even when an item throws.
        var images = new TranscodedImage[work.Count];
        Parallel.ForEach(
            work,
            NativeMethods.bt_ktx2_create_transcode_state,
            (item, _, index, stateHandle) =>
            {
                images[index] = Transcode(ktx2File, format, item.Level, item.Layer, item.Face, flags, stateHandle);
                return stateHandle;
            },
            NativeMethods.bt_ktx2_destroy_transcode_state);

        return images;
    }

    private static TranscodedImage Transcode(Ktx2FileHandle ktx2File, TranscodeFormat format, uint level, uint layer, uint face, DecodeFlags flags, ulong stateHandle)
    {
        var width = NativeMethods.bt_ktx2_get_level_orig_width(ktx2File, level, layer, face);
        var height = NativeMethods.bt_ktx2_get_level_orig_height(ktx2File, level, layer, face);
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
            level, layer, face,
            data,
            sizeInBytes / bytesPerUnit,
            format,
            flags,
            outputRowPitchInBlocksOrPixels: 0,
            outputRowsInPixels: 0,
            channel0: -1, channel1: -1,
            stateHandle);

        if (!succeeded)
        {
            throw new InvalidOperationException($"Failed to transcode level {level}, layer {layer}, face {face} from {GetFormat(ktx2File)} to {format}");
        }

        return new TranscodedImage(format, level, layer, face, width, height, rowPitch, data);
    }
}
