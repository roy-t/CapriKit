# Texture2D pipeline

Context: adding 2D texture support to the asset pipeline (`Texture2DTranscoder` in
`CapriKit.AssetPipeline.DirectX11/Textures`). Source images are encoded offline to KTX2 with
`CapriKit.SuperCompressed` (a wrapper around basis_universal's plain-C `bu_*`/`bt_*` API), and
transcoded to a BCn format when loaded into a DirectX 11 `Texture2D`.

## Texture usages and their settings

| Usage | `Srgb` flag | Encoder format | Preprocess | Transcode to | DXGI format | Decode flags |
|---|---|---|---|---|---|---|
| `SRgb` (color) | on | `UastcLdr4x4` | none | `Bc7Rgba` | `BC7_UNORM_SRGB` | none |
| `Linear` (data) | off | `UastcLdr4x4` | none | `Bc7Rgba` (or `Bc4R` for one channel) | `BC7_UNORM` (`BC4_UNORM`) | none |
| `NormalMap` | off | `UastcLdr4x4` | renormalize, swizzle to `XXXY` | `Bc5Rg` | `BC5_UNORM` | `HighQuality` |

Encoder flags common to all usages: `Threaded | Ktx2UastcZstd`, plus `GenMipsClamp` or `GenMipsWrap`
(match the sampler's address mode). Quality is `100`.

### What the `Srgb` compression flag does

The C API exposes a single sRGB switch (see `basis_compress_internal` in `basisu_comp.cpp`). It sets:

- `m_perceptual`: perceptual error metrics instead of PSNR.
- `m_ktx2_and_basis_srgb_transfer_function`: the transfer function written to the KTX2 header (and the
  ASTC/XUASTC decode profile).
- `m_mip_srgb`: mips are filtered in linear space.
- `set_ldr_srgb_channel_weights`: Rec.709 channel weights instead of equal weights.

It does **not** make the GPU linearize on sampling, that only happens when the view format is `*_SRGB`.
`Ktx2Transcoder.IsSrgb` reads the transfer function back, `Decode` asserts it matches the settings to
catch stale assets.

The old basisu settings `m_renormalize` and `m_mip_renormalize` (and swizzling) are **not** exposed by the
C API, which is why normal maps are preprocessed in C#.

## Normal maps

- Renormalize every source pixel (fixes sloppy authoring), then store X in R, G and B and Y in A (`XXXY`,
  basisu's own `-separate_rg_to_color_alpha`). Drop Z.
- Why `XXXY` instead of `XYZ`: block compression stores per-block endpoints with one index per pixel that is
  shared by R, G and B, so it works well when colors lie on a line through RGB space. X and Y are
  uncorrelated and don't. Alpha has its own endpoints and indices in every relevant format (BC3/BC7 alpha,
  UASTC dual-plane, ETC1S alpha slice). Duplicating X puts it on the gray diagonal (a perfect line), Z is
  redundant, so all bits go to X and Y.
- BC5 is two independent BC4 halves, sampled as `.r` and `.g`. The transcoder picks the source channels:
  (X)UASTC uses channel 0 (R) and 3 (A), ETC1S uses the green of the color slice and the green of the alpha
  slice. So `A -> G` is a transcoder choice, not a GPU feature, and duplicating X makes either path work.
- `DecodeFlags.HighQuality` is passed to `transcode_uastc_to_bc5`/`bc4`, it has no effect on UASTC -> BC7.
- Shader reconstruction:

  ```hlsl
  float2 xy = NormalMap.Sample(Sampler, uv).rg * 2.0f - 1.0f;
  float3 n = float3(xy, sqrt(saturate(1.0f - dot(xy, xy))));
  ```

- Mip renormalization isn't needed with BC5: averaged mips get a shorter `xy` and reconstructing `z` makes
  the normal unit length again (it just flattens, which is the expected look for a mip). If explicitly
  renormalized mips are ever wanted: `bu_comp_params_set_image_rgba32` with `image_index > 0` accepts
  precomputed mip levels for 2D textures.
- Gotcha: the encoder drops alpha when it is 255 everywhere (`m_check_for_alpha`), only a degenerate normal
  map would trigger this.

## ETC1S vs UASTC LDR 4x4

Rough figures from the basisu documentation, not measured on our content:

| | ETC1S | UASTC LDR 4x4 |
|---|---|---|
| Disk size | ~1-2 bpp | 8 bpp raw, ~5-6 bpp with Zstd, ~2-4 bpp with RDO + Zstd |
| Quality | around BC1/ETC1, blocking and banding | close to BC7 (1-2 dB PSNR below) |
| GPU memory | 4 bpp as BC1, 8 bpp as BC7 | 8 bpp as BC7 |

- Both transcode to the same GPU formats, so the difference is mostly on disk. ETC1S -> BC7 wastes VRAM at
  ETC1S quality.
- ETC1S is bad for gradients, normal maps and packed data (it shares codebooks across blocks, which mixes
  unrelated channels).
- Decision: UASTC LDR 4x4 for everything, disk size matters little for a desktop D3D11 project. ETC1S is
  only worth it when download size counts. XUASTC LDR sits in between and is worth measuring later.

## Quality, RDO and Zstd

- For `UastcLdr4x4`, any `quality < 100` enables RDO (`set_format_mode_and_quality_effort` in
  `basisu_comp.cpp`). RDO trades quality for better compressibility by Zstd, without
  `Ktx2UastcZstd` it only loses quality.
- Decision: always set `Ktx2UastcZstd` (lossless) and use quality `100`. Lowering quality for color
  textures is an option later; keep normal maps at or near `100`.

## Decoding all mip levels

- `Ktx2Transcoder.TranscodeAll` transcodes every level/layer/face in parallel (one native transcode state
  per worker).
- `Texture2D` takes a `ReadOnlySpan<MipLevel>` (`MipLevel(ReadOnlyMemory<byte> Data, uint RowPitch)`),
  keeping `CapriKit.DirectX11` free of a `SuperCompressed` dependency. It creates an `Immutable` texture in
  one `CreateTexture2D` call with one `SubresourceData` per level. `HotSwap` replaces the whole texture, so
  immutability is fine.
- Each level is pinned with `Memory<byte>.Pin()` only until `CreateTexture2D` returns, D3D11 copies the
  initial data during that call.
- Use the transcoder's `RowPitch`: for BC formats it is the size of one row of 4x4 blocks.
- Mapping `TranscodedImage` to `MipLevel` does not copy texel data, `byte[] -> ReadOnlyMemory<byte>` only
  wraps the reference. The real copies are `reader.ReadBytes` and `Ktx2Transcoder.Open` (which copies to
  the pinned object heap because the native transcoder keeps a pointer), plus the driver upload.
- Tiny levels (2x2, 1x1) are still one 4x4 block, the transcoder and D3D11 handle this.
- Test: `Texture2DTests.CreateWithMipLevels` creates an 8x8 BC7 texture with two levels.

## Open points

- `TranscodeAll` orders by level -> layer -> face, D3D11 subresources are `mip + arraySlice * mipLevels`.
  Arrays and cubemaps need reordering before they can use the same path.
- D3D11 requires the top level of a BC texture to be a multiple of 4 in width and height, validate in
  `Encode` for a clearer error.
- The compressed data is copied twice on decode (`ReadBytes`, `Open`). `Open` could accept an
  already-pinned array to skip one copy.
- `ReadSettings`/`WriteSettings` are not implemented yet.
- Measure ETC1S/UASTC/XUASTC size and PSNR on our own textures (`PrintStats` prints basisu's metrics).
