using System.Runtime.InteropServices;

namespace CapriKit.SuperCompressed;

/// <summary>
/// P/Invoke bindings for caprikit_basisu.dll, a 1:1 mirror of basis_universal's C API.
/// Method names match the native entry points so the upstream headers double as documentation:
/// external/basis_universal/encoder/basisu_wasm_api.h (bu_*, encoder) and
/// basisu_wasm_transcoder_api.h (bt_*, transcoder).
/// </summary>
/// <remarks>
/// The native API models memory as uint64 "offsets" for WASM compatibility; in this native
/// build they are simply raw pointers. Managed memory is pinned (spans) or allocated by the
/// caller, so the native bu_alloc/bu_free/bt_alloc/bt_free helpers are deliberately not bound.
/// wasm_bool_t is a uint32, marshalled as a 4-byte bool.
/// </remarks>
internal static partial class NativeMethods
{
    private const string DllName = "caprikit_basisu";

    /// <summary>
    /// The BASISU_LIB_VERSION this wrapper was written against; bu_get_version() and
    /// bt_get_version() must match. Update when updating the basis_universal submodule.
    /// </summary>
    public const uint ExpectedLibVersion = 250;

    // The encoder and transcoder must be initialized once before any other call.
    static NativeMethods()
    {
        bu_init();
        bt_init();
    }

    // ---- Encoder (basisu_wasm_api.h) ----

    /// <remarks>Also prints a greeting to stdout; do not call in library code paths.</remarks>
    [LibraryImport(DllName)]
    public static partial uint bu_get_version();

    [LibraryImport(DllName)]
    public static partial void bu_enable_debug_printf([MarshalAs(UnmanagedType.U4)] bool flag);

    [LibraryImport(DllName)]
    private static partial void bu_init();

    [LibraryImport(DllName)]
    public static partial CompParamsHandle bu_new_comp_params();

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bu_delete_comp_params(IntPtr paramsHandle);

    [LibraryImport(DllName)]
    public static partial ulong bu_comp_params_get_comp_data_size(CompParamsHandle paramsHandle);

    /// <remarks>
    /// Returns a pointer into native memory owned by the params object; it is invalidated
    /// by the next clear, compress or delete call on the same params object.
    /// </remarks>
    [LibraryImport(DllName)]
    public static partial IntPtr bu_comp_params_get_comp_data_ofs(CompParamsHandle paramsHandle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bu_comp_params_clear(CompParamsHandle paramsHandle);

    /// <summary>Copies a 32bpp RGBA image into the params object (image data is not retained).</summary>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bu_comp_params_set_image_rgba32(
        CompParamsHandle paramsHandle,
        uint imageIndex,
        ReadOnlySpan<byte> imageData,
        uint width, uint height,
        uint pitchInBytes);

    /// <summary>Copies a 128bpp float RGBA image into the params object (image data is not retained).</summary>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bu_comp_params_set_image_float_rgba(
        CompParamsHandle paramsHandle,
        uint imageIndex,
        ReadOnlySpan<byte> imageData,
        uint width, uint height,
        uint pitchInBytes);

    /// <summary>
    /// Compresses the images previously set on the params object. On success the result is
    /// available via bu_comp_params_get_comp_data_size/_ofs. Quality is 1-100, effort 0-10.
    /// </summary>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bu_compress_texture(
        CompParamsHandle paramsHandle,
        BasisTexFormat desiredBasisTexFormat,
        int qualityLevel, int effortLevel,
        CompressionFlags flags,
        float lowLevelUastcRdoOrDctQuality);

    // ---- Transcoder (basisu_wasm_transcoder_api.h) ----

    /// <remarks>Also prints a greeting to stdout; do not call in library code paths.</remarks>
    [LibraryImport(DllName)]
    public static partial uint bt_get_version();

    [LibraryImport(DllName)]
    public static partial void bt_enable_debug_printf([MarshalAs(UnmanagedType.U4)] bool flag);

    [LibraryImport(DllName)]
    private static partial void bt_init();

    // basis_tex_format helpers

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_tex_format_is_xuastc_ldr(BasisTexFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_tex_format_is_astc_ldr(BasisTexFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_tex_format_is_xubc7(BasisTexFormat format);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_tex_format_get_block_width(BasisTexFormat format);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_tex_format_get_block_height(BasisTexFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_tex_format_is_hdr(BasisTexFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_tex_format_is_ldr(BasisTexFormat format);

    // transcoder_texture_format helpers

    [LibraryImport(DllName)]
    public static partial uint bt_basis_get_bytes_per_block_or_pixel(TranscodeFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_transcoder_format_has_alpha(TranscodeFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_transcoder_format_is_hdr(TranscodeFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_transcoder_format_is_ldr(TranscodeFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_transcoder_texture_format_is_astc(TranscodeFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_transcoder_format_is_uncompressed(TranscodeFormat format);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_get_uncompressed_bytes_per_pixel(TranscodeFormat format);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_get_block_width(TranscodeFormat format);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_get_block_height(TranscodeFormat format);

    [LibraryImport(DllName)]
    public static partial TranscodeFormat bt_basis_get_transcoder_texture_format_from_basis_tex_format(BasisTexFormat format);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_basis_is_format_supported(TranscodeFormat transcodeFormat, BasisTexFormat basisTexFormat);

    [LibraryImport(DllName)]
    public static partial uint bt_basis_compute_transcoded_image_size_in_bytes(TranscodeFormat format, uint origWidth, uint origHeight);

    // KTX2 files

    /// <remarks>
    /// The native side keeps referencing <paramref name="data"/> after this call: the memory
    /// must stay valid (and, if managed, pinned) until the returned handle is disposed.
    /// </remarks>
    [LibraryImport(DllName)]
    public static partial Ktx2FileHandle bt_ktx2_open(IntPtr data, uint dataLength);

    [LibraryImport(DllName)]
    public static partial void bt_ktx2_close(IntPtr ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_width(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_height(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_levels(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_faces(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_layers(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial BasisTexFormat bt_ktx2_get_basis_tex_format(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_etc1s(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_uastc_ldr_4x4(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_hdr(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_hdr_4x4(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_hdr_6x6(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_ldr(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_astc_ldr(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_xuastc_ldr(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_xubc7(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_block_width(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_block_height(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_deblocking_filter_index(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_has_alpha(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_color_model(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_color_primaries(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_transfer_func(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_srgb(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_flags(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_total_samples(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_channel_id0(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_dfd_channel_id1(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_is_video(Ktx2FileHandle ktx2Handle);

    [LibraryImport(DllName)]
    public static partial float bt_ktx2_get_ldr_hdr_upconversion_nit_multiplier(Ktx2FileHandle ktx2Handle);

    // KTX2 levels

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_orig_width(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_orig_height(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_actual_width(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_actual_height(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_num_blocks_x(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_num_blocks_y(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    public static partial uint bt_ktx2_get_level_total_blocks(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_get_level_alpha_flag(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_get_level_iframe_flag(Ktx2FileHandle ktx2Handle, uint levelIndex, uint layerIndex, uint faceIndex);

    // Transcoding

    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_start_transcoding(Ktx2FileHandle ktx2Handle);

    /// <remarks>Only needed for transcoding the same file from multiple threads; pass 0 otherwise.</remarks>
    [LibraryImport(DllName)]
    public static partial ulong bt_ktx2_create_transcode_state();

    [LibraryImport(DllName)]
    public static partial void bt_ktx2_destroy_transcode_state(ulong stateHandle);

    /// <summary>
    /// Transcodes one mip level of one layer/face into <paramref name="outputBlocks"/>,
    /// whose size is given in blocks (compressed formats) or pixels (uncompressed formats).
    /// </summary>
    [LibraryImport(DllName)]
    [return: MarshalAs(UnmanagedType.U4)]
    public static partial bool bt_ktx2_transcode_image_level(
        Ktx2FileHandle ktx2Handle,
        uint levelIndex, uint layerIndex, uint faceIndex,
        Span<byte> outputBlocks,
        uint outputBlocksSizeInBlocksOrPixels,
        TranscodeFormat format,
        DecodeFlags decodeFlags,
        uint outputRowPitchInBlocksOrPixels,
        uint outputRowsInPixels,
        int channel0, int channel1,
        ulong stateHandle);
}
