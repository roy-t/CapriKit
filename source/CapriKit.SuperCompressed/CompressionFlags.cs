namespace CapriKit.SuperCompressed;

/// <summary>
/// Flags for encoding, mirrors the BU_COMP_FLAGS_* constants in
/// external/basis_universal/encoder/basisu_wasm_api_common.h.
/// The native parameter is 64 bits wide, so this enum is backed by ulong.
/// </summary>
/// <remarks>
/// Bits 23-24 (XUASTC LDR syntax), 25-26 (texture type) and 29-31 (XUBC7 base encoder)
/// are multi-bit fields rather than independent flags. The default (0) means: full
/// arithmetic XUASTC syntax, 2D texture, BC7F base encoder.
/// </remarks>
[Flags]
public enum CompressionFlags : ulong
{
    None = 0,
    UseOpenCL = 1ul << 8,
    Threaded = 1ul << 9,
    DebugOutput = 1ul << 10,
    Ktx2Output = 1ul << 11,
    Ktx2UastcZstd = 1ul << 12,
    Srgb = 1ul << 13,
    GenMipsClamp = 1ul << 14,
    GenMipsWrap = 1ul << 15,
    YFlip = 1ul << 16,
    PrintStats = 1ul << 18,
    PrintStatus = 1ul << 19,
    DebugImages = 1ul << 20,
    Rec2020 = 1ul << 21,
    ValidateOutput = 1ul << 22,
    XuastcLdrSyntaxHybrid = 1ul << 23,
    XuastcLdrSyntaxFullZstd = 2ul << 23,
    TextureType2DArray = 1ul << 25,
    TextureTypeCubemapArray = 2ul << 25,
    TextureTypeVideoFrames = 3ul << 25,
    DisableDeblocking = 1ul << 27,
    ForceDeblocking = 1ul << 28,
}
