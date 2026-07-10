namespace CapriKit.SuperCompressed;

// Mirrors the BTF_* constants (basist::basis_tex_format) in
// external/basis_universal/encoder/basisu_wasm_api_common.h.
/// <summary>The intermediate texture formats a .ktx2 file can store.</summary>
public enum BasisTexFormat : uint
{
    Etc1s = 0,
    UastcLdr4x4 = 1,
    UastcHdr4x4 = 2,
    AstcHdr6x6 = 3,
    UastcHdr6x6 = 4,
    XuastcLdr4x4 = 5,
    XuastcLdr5x4 = 6,
    XuastcLdr5x5 = 7,
    XuastcLdr6x5 = 8,
    XuastcLdr6x6 = 9,
    XuastcLdr8x5 = 10,
    XuastcLdr8x6 = 11,
    XuastcLdr10x5 = 12,
    XuastcLdr10x6 = 13,
    XuastcLdr8x8 = 14,
    XuastcLdr10x8 = 15,
    XuastcLdr10x10 = 16,
    XuastcLdr12x10 = 17,
    XuastcLdr12x12 = 18,
    AstcLdr4x4 = 19,
    AstcLdr5x4 = 20,
    AstcLdr5x5 = 21,
    AstcLdr6x5 = 22,
    AstcLdr6x6 = 23,
    AstcLdr8x5 = 24,
    AstcLdr8x6 = 25,
    AstcLdr10x5 = 26,
    AstcLdr10x6 = 27,
    AstcLdr8x8 = 28,
    AstcLdr10x8 = 29,
    AstcLdr10x10 = 30,
    AstcLdr12x10 = 31,
    AstcLdr12x12 = 32,
    Xubc7 = 33,
}
