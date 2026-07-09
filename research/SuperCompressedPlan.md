# CapriKit.SuperCompressed — Plan & Research Findings

_Follow-up to `TextureProcessing.md`. Investigated 2026-07-09. Goal: wrap basis_universal
(`external/basis_universal` submodule) in C# for the asset pipeline: load images, generate
mipmaps, encode to a supercompressed container, and transcode to GPU-native formats for DirectX11._

## Key findings

### 1. basis_universal now ships an official plain-C API
The current submodule contains a C API maintained by Binomial themselves, which removes the
need for any custom C++ glue:

- `encoder/basisu_wasm_api.h` — encoder API, `bu_*` functions. Feed raw RGBA32 (or float RGBA)
  pixels via `bu_comp_params_set_image_rgba32`, then `bu_compress_texture` with a
  `basis_tex_format` (ETC1S/UASTC/XUASTC/…), quality 1–100, effort 0–10, and flags
  (`BU_COMP_FLAGS_*`: KTX2 output, sRGB, threading, mipmap generation with clamp/wrap).
- `encoder/basisu_wasm_transcoder_api.h` — transcoder API, `bt_*` functions. Opens `.ktx2`
  blobs (`bt_ktx2_open`), queries width/height/levels/layers/faces/format/alpha, computes
  output sizes (`bt_basis_compute_transcoded_image_size_in_bytes`), and transcodes any
  level/layer/face to GPU formats (`bt_ktx2_transcode_image_level`): BC1/BC3/BC4/BC5/BC7,
  BC6H, ASTC, ETC, RGBA32, …
- Constants live in `encoder/basisu_wasm_api_common.h` (`BTF_*` source formats, `TF_*`
  transcode targets, `BU_COMP_FLAGS_*`, `DECODE_FLAGS_*`, quality/effort ranges).

Despite the "wasm" file names the API compiles natively (`example_capi.exe` is built from it;
the Python bindings use the same files as a native shared library). Versioned via
`bu_get_version()` (`BASISU_LIB_VERSION`, currently 250 in the checked-out submodule).
Note: `bu_get_version()` also printf's a "Hello from basisu_wasm_api.cpp" line to stdout.

**On native builds the API's `uint64` "offsets" are just raw pointers** (`wasm_ptr` casts them
back). So from C# we can pin managed memory and pass the pointer as `ulong` — no `bu_alloc`/
`bt_free` round-trips, no double copies. The encoder copies pixels into its own storage during
`set_image`, so pinning only for the call duration is safe.

### 2. The old SuperCompressed project was not C++/CLI
`C:\projects\csharp\SuperCompressed` is a plain native DLL (`SuperCompressed.Native`,
`extern "C"` exports) consumed via `DllImport` (`NativeMethods.cs`). The pain point was the
~500 lines of hand-written C++ glue duplicating basisu logic — which the upstream C API now
makes obsolete. Its C# API shape (Image/Encoder/Transcoder, spans in, no raw pointers out)
is still a good reference. It used StbImageSharp for image loading and ZstdSharp to compress
the `.basis` output (unnecessary with KTX2: `BU_COMP_FLAGS_KTX2_UASTC_ZSTD` supercompresses
inside the container).

### 3. Integration options considered

| Option | Verdict |
|---|---|
| P/Invoke against upstream's C API | **Chosen** — near-zero custom native code, upstream-maintained surface |
| C++/CLI | No — runtime-coupled, solves a problem we no longer have |
| Custom C shim + P/Invoke (old approach) | Obsolete; keep in back pocket for API gaps |
| Host upstream's `.wasm` via wasmtime-dotnet | Zero native build, but slow encoding; not primary |
| ClangSharp/CppSharp codegen | Overkill for ~90 simple functions |
| Community NuGet bindings | Stale versions defeat the "easy updates" goal |

### 4. Native build: CMake, reusing upstream's build definitions
Our own `CMakeLists.txt` can `add_subdirectory(external/basis_universal EXCLUDE_FROM_ALL)`
and link upstream's `basisu_encoder` static-library target — upstream maintains the source
list and compiler flags (MSVC defaults are already right: SSE on, Zstd on, OpenCL off).
`EXCLUDE_FROM_ALL` skips their tools/examples. The whole native build we own is ~10 lines:

```cmake
cmake_minimum_required(VERSION 3.20)
project(caprikit_basisu CXX)

set(BASISU_EXAMPLES OFF)
add_subdirectory(<repo>/external/basis_universal basisu EXCLUDE_FROM_ALL)

add_library(caprikit_basisu SHARED
    <repo>/external/basis_universal/encoder/basisu_wasm_api.cpp
    <repo>/external/basis_universal/encoder/basisu_wasm_transcoder_api.cpp
    exports.def)
target_link_libraries(caprikit_basisu PRIVATE basisu_encoder)
```

This mirrors how upstream builds its own `example_capi` target. Details:

- `exports.def` is required: `BU_WASM_EXPORT` expands to bare `extern "C"` on MSVC (no
  `__declspec(dllexport)`). The `.def` file is just the ~90 `bu_*`/`bt_*` names copied from
  the two headers; regenerate when the headers change.
- A **vcxproj is not an option**: `dotnet build` cannot build C++ projects (MSB4278 —
  `Microsoft.Cpp.Default.props` only exists in Visual Studio's MSBuild). Verified: the
  `basisu.vcxproj` entry currently in `CapriKit.slnx` breaks solution-level `dotnet build`
  and should be removed.
- A thin `build.ps1` wraps the two cmake commands (configure + `--build --config Release`)
  and copies the DLL. Rebuild is only needed when the submodule updates. GitHub Actions
  windows runners ship CMake + MSVC, so CI can build it for the unit tests.

### 5. API gaps in the upstream C API (accepted for v1)
- **No image file loading** — takes raw pixels only. Decision: use **StbImageSharp**
  (managed; jpg/png/bmp/tga/psd/gif), same as the old project.
- **No mipmap filter selection** — only on/off + clamp/wrap flags; upstream's default
  filter (kaiser) is used. Decision: accept the default; per-level custom mips aren't
  expressible in the C API either. Revisit via a tiny shim or upstream PR if ever needed.
- **KTX2 only** — the C transcoder API has no `.basis` reader. Decision: standardize on
  `.ktx2` (upstream's recommended container anyway).

## Decisions (agreed 2026-07-09)

- P/Invoke (`[LibraryImport]`) against upstream's `bu_*`/`bt_*` C API; no custom native code
  beyond a CMakeLists + `.def` file.
- **Windows/x64 only** — the engine is DirectX11-based; no cross-platform requirement.
- **KTX2 only**, StbImageSharp for image loading, default kaiser mip filter.
- Native DLL name: `caprikit_basisu.dll`.

## Implementation plan

1. **`source/CapriKit.SuperCompressed.Native/`** — `CMakeLists.txt` (as above), `exports.def`,
   `build.ps1`. Produces `caprikit_basisu.dll` (x64 Release). Never modifies the submodule.
2. **Interop layer** (internal, in `CapriKit.SuperCompressed`) — `NativeMethods.cs` with
   `[LibraryImport]` mirroring the two headers 1:1; `ulong` handles wrapped in `SafeHandle`s
   (`CompParamsHandle`, `Ktx2Handle`); enums mirroring `BTF_*`/`TF_*`/flags constants;
   one-time `bu_init`/`bt_init` + `bu_get_version()` check.
3. **Public API** (span-based, no pointers):
   - `Image` — RGBA32 pixels + dimensions; `Image.Load(stream/bytes)` via StbImageSharp.
   - `Encoder.Encode(Image, BasisTexFormat, Quality, Effort, options) → byte[]` (`.ktx2` blob).
   - `Ktx2Transcoder : IDisposable` — `Open(ReadOnlySpan<byte>)`; `Width/Height/Levels/Layers/
     Faces/Format/HasAlpha`; `Transcode(level, layer, face, TranscodeFormat) → TranscodedImage`
     (byte[], dimensions, pitch).
   - `TranscodeFormat → DXGI_FORMAT` mapping belongs in `CapriKit.DirectX11`, not here.
4. **Tests** (TUnit, `CapriKit.Tests`, happy path) — encode a small generated gradient with
   mips → open → assert level count/dimensions → transcode to BC7 and RGBA32 → assert buffer
   sizes match `bt_basis_compute_transcoded_image_size_in_bytes`.
5. **Packaging/docs** — DLL as `runtimes/win-x64/native` in the NuGet; project README with the
   submodule-update procedure (bump submodule → regen `.def` if headers changed → rebuild DLL).

First milestone: build the DLL and get a `bu_get_version` smoke test passing from C#.

## Environment verification (2026-07-09)

- `dotnet build` — all 13 C# projects build; solution-level build **fails** only on the
  `external/basis_universal/basisu.vcxproj` entry in `CapriKit.slnx` (remove it).
- CMake — **not on PATH**; use `C:\Program Files\CMake\bin\cmake.exe`. Generator
  "Visual Studio 18 2026", MSVC 19.51.
- basis_universal — configures and builds cleanly (`basisu_encoder.lib`, Release x64).
  Note: MSVC warns (MSB8029) against build dirs under Temp; use a repo-local build dir.
- `dotnet test source\CapriKit.Tests\CapriKit.Tests.csproj` — 189/189 pass.
