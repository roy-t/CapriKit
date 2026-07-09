namespace CapriKit.SuperCompressed;

/// <summary>
/// One transcoded mip level: texel data ready to upload to the GPU.
/// </summary>
/// <param name="Format">The GPU texture format of <paramref name="Data"/>.</param>
/// <param name="Level">The mip level this image was transcoded from.</param>
/// <param name="Layer">The array layer this image was transcoded from; 0 for textures that are not arrays.</param>
/// <param name="Face">The cubemap face this image was transcoded from; 0 for textures that are not cubemaps.</param>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="RowPitch">
/// Distance in bytes between consecutive rows of pixels (uncompressed formats) or rows of
/// blocks (compressed formats), as expected by for example D3D11_SUBRESOURCE_DATA.SysMemPitch.
/// </param>
/// <param name="Data">The texel data.</param>
public sealed record TranscodedImage(TranscodeFormat Format, uint Level, uint Layer, uint Face, uint Width, uint Height, uint RowPitch, byte[] Data);
