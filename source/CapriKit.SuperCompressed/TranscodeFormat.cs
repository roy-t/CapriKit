namespace CapriKit.SuperCompressed;

/// <summary>
/// The GPU texture formats a .ktx2 file can be transcoded to, mirrors the TF_* constants in
/// external/basis_universal/encoder/basisu_wasm_api_common.h (basist::transcoder_texture_format).
/// </summary>
public enum TranscodeFormat : uint
{
    Etc1Rgb = 0,
    Etc2Rgba = 1,
    Bc1Rgb = 2,
    Bc3Rgba = 3,
    Bc4R = 4,
    Bc5Rg = 5,
    Bc7Rgba = 6,
    Pvrtc14Rgb = 8,
    Pvrtc14Rgba = 9,
    AstcLdr4x4Rgba = 10,
    AtcRgb = 11,
    AtcRgba = 12,
    Rgba32 = 13,
    Rgb565 = 14,
    Bgr565 = 15,
    Rgba4444 = 16,
    Fxt1Rgb = 17,
    Pvrtc24Rgb = 18,
    Pvrtc24Rgba = 19,
    Etc2EacR11 = 20,
    Etc2EacRg11 = 21,
    Bc6h = 22,
    AstcHdr4x4Rgba = 23,
    RgbHalf = 24,
    RgbaHalf = 25,
    Rgb9e5 = 26,
    AstcHdr6x6Rgba = 27,
    AstcLdr5x4Rgba = 28,
    AstcLdr5x5Rgba = 29,
    AstcLdr6x5Rgba = 30,
    AstcLdr6x6Rgba = 31,
    AstcLdr8x5Rgba = 32,
    AstcLdr8x6Rgba = 33,
    AstcLdr10x5Rgba = 34,
    AstcLdr10x6Rgba = 35,
    AstcLdr8x8Rgba = 36,
    AstcLdr10x8Rgba = 37,
    AstcLdr10x10Rgba = 38,
    AstcLdr12x10Rgba = 39,
    AstcLdr12x12Rgba = 40,
}
