namespace CapriKit.SuperCompressed;

/// <summary>
/// Reads a KTX2 texture (as produced by <see cref="Encoder"/>) and transcodes its
/// mip levels to GPU-native formats.
/// </summary>
public sealed class Ktx2Transcoder : IDisposable
{
    private readonly Ktx2FileHandle handle;

    // The native KTX2 reader references this buffer for the lifetime of the handle,
    // so it is allocated on the pinned object heap, where it never moves.
    private readonly byte[] fileData;

    private Ktx2Transcoder(Ktx2FileHandle handle, byte[] fileData)
    {
        this.handle = handle;
        this.fileData = fileData;
    }

    /// <summary>Parses the contents of a .ktx2 file and prepares it for transcoding.</summary>
    /// <exception cref="InvalidDataException">The data is not a supported KTX2 texture.</exception>
    public static unsafe Ktx2Transcoder Open(ReadOnlySpan<byte> ktx2Data)
    {
        var pinnedData = GC.AllocateUninitializedArray<byte>(ktx2Data.Length, pinned: true);
        ktx2Data.CopyTo(pinnedData);

        Ktx2FileHandle handle;
        fixed (byte* pData = pinnedData)
        {
            handle = NativeMethods.bt_ktx2_open((IntPtr)pData, (uint)pinnedData.Length);
        }

        if (handle.IsInvalid)
        {
            throw new InvalidDataException("Not a valid KTX2 file");
        }

        if (!NativeMethods.bt_ktx2_start_transcoding(handle))
        {
            handle.Dispose();
            throw new InvalidDataException("Failed to prepare the KTX2 file for transcoding");
        }

        return new Ktx2Transcoder(handle, pinnedData);
    }

    /// <summary>Width in pixels of the largest mip level.</summary>
    public int Width => (int)NativeMethods.bt_ktx2_get_width(handle);

    /// <summary>Height in pixels of the largest mip level.</summary>
    public int Height => (int)NativeMethods.bt_ktx2_get_height(handle);

    /// <summary>The number of mip levels.</summary>
    public int Levels => (int)NativeMethods.bt_ktx2_get_levels(handle);

    /// <summary>The number of array layers; 0 for textures that are not arrays.</summary>
    public int Layers => (int)NativeMethods.bt_ktx2_get_layers(handle);

    /// <summary>The number of faces: 6 for cubemaps, 1 otherwise.</summary>
    public int Faces => (int)NativeMethods.bt_ktx2_get_faces(handle);

    /// <summary>The intermediate texture format stored inside the KTX2 file.</summary>
    public BasisTexFormat Format => NativeMethods.bt_ktx2_get_basis_tex_format(handle);

    public bool HasAlpha => NativeMethods.bt_ktx2_has_alpha(handle);

    public bool IsSrgb => NativeMethods.bt_ktx2_is_srgb(handle);

    /// <summary>Transcodes one mip level to the given GPU texture format.</summary>
    public TranscodedImage Transcode(TranscodeFormat format, int level = 0, int layer = 0, int face = 0, DecodeFlags flags = DecodeFlags.None)
    {
        var width = NativeMethods.bt_ktx2_get_level_orig_width(handle, (uint)level, (uint)layer, (uint)face);
        var height = NativeMethods.bt_ktx2_get_level_orig_height(handle, (uint)level, (uint)layer, (uint)face);
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
            handle,
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
            throw new InvalidOperationException($"Failed to transcode level {level}, layer {layer}, face {face} from {Format} to {format}");
        }

        return new TranscodedImage(format, (int)width, (int)height, (int)rowPitch, data);
    }

    public void Dispose()
    {
        handle.Dispose();
    }
}
