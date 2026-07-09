namespace CapriKit.SuperCompressed;

/// <summary>
/// One transcoded mip level: texel data ready to upload to the GPU.
/// </summary>
/// <param name="Format">The GPU texture format of <paramref name="Data"/>.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="RowPitch">
/// Distance in bytes between consecutive rows of pixels (uncompressed formats) or rows of
/// blocks (compressed formats), as expected by for example D3D11_SUBRESOURCE_DATA.SysMemPitch.
/// </param>
/// <param name="Data">The texel data.</param>
public sealed record TranscodedImage(TranscodeFormat Format, int Width, int Height, int RowPitch, byte[] Data);
