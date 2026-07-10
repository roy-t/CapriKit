namespace CapriKit.SuperCompressed;

// Mirrors the DECODE_FLAGS_* constants (basist::basisu_decode_flags) in
// external/basis_universal/encoder/basisu_wasm_api_common.h.
/// <summary>Options for the <see cref="Ktx2Transcoder"/> transcode methods.</summary>
[Flags]
public enum DecodeFlags : uint
{
    None = 0,
    PvrtcDecodeToNextPow2 = 2,
    TranscodeAlphaDataToOpaqueFormats = 4,
    Bc1ForbidThreeColorBlocks = 8,
    OutputHasAlphaIndices = 16,
    HighQuality = 32,
    NoEtc1sChromaFiltering = 64,
    NoDeblockFiltering = 128,
    ForceDeblockFiltering = 512,
    XuastcLdrDisableFastBc7Transcoding = 1024,
}
