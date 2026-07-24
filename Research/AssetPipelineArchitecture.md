# Asset Pipeline — High-Level Architecture

Companion to `AssetPipeline.md` (the brief). Status: draft for discussion, 2026-07-10.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Build timing | **Hybrid** | Dev builds compile stale assets on demand into a cache; a CLI step produces the same output for shipping. Shipped games load precompiled assets only and carry no compiler code. |
| Asset relationships | **Flat** | No cross-asset references. Game code composes (loads a model's geometry and its textures separately). Kills the dependency-graph machinery that made MiniEngine3 complex. |
| GPU coupling | **Direct DX11 dependency** | The pipeline references `CapriKit.DirectX11` and produces GPU resources itself. Fewer abstractions to understand and maintain. |
| Shaders | **Offline bytecode** | HLSL → DXBC at compile time. `CapriKit.Generators.HLSL` keeps generating the C#-side metadata (structs, input element descriptions, entry points) unchanged; the pipeline only takes over bytecode compilation. |

## Big Picture

```
 source tree                 compiled tree                runtime
 (loose files)               (loose files, mirrored)
 ┌──────────────┐  compile   ┌──────────────┐   load     ┌───────────────┐
 │ grass.png    │ ─────────► │ grass.cka    │ ─────────► │ Texture2D     │
 │ basic.hlsl   │  IAsset-   │ basic.cka    │  IAsset-   │ VertexShader..│
 │ tree.gltf    │  Compiler  │ tree.cka     │  Loader<T> │ Model         │
 │ click.wav    │            │ click.cka    │            │ AudioClip     │
 └──────────────┘            └──────────────┘            └───────────────┘
   dev machine only           dev cache == ship content    game process
```

Two phases, two families of pluggable pieces:

- **Compilers** (dev machine / build server): source format → compiled container. Slow, thorough, offline.
- **Loaders** (game process): compiled container → ready-to-use object (texture on the GPU, playable audio clip). Fast, allocation-conscious.

An asset is identified by its **source-relative path**, and every source file maps 1:1 to
one compiled file with the same relative path (different extension). This 1:1 rule is what
keeps identity, staleness checks, and file watching trivial.

## Phase 1: Compiling

```csharp
public interface IAssetCompiler
{
    IReadOnlySet<string> SupportedExtensions { get; }
    int Version { get; }

    // Reads source via a *tracking* file system so every file touched
    // (e.g. #included HLSL) is recorded as an input of this asset.
    void Compile(AssetId id, ITrackingReadOnlyFileSystem source, Stream output);
}
```

### Compiled container (`.cka` — "CapriKit Asset")

One small binary envelope shared by all types, so staleness and loading logic is written once:

- Header: magic, container version, compiler type + version, hashes of all input files.
- Body: one or more named blobs (a KTX2 payload; a DXBC blob per shader entry point; vertex + index buffers).

### Staleness & the hybrid model

An asset is stale when the compiled file is missing, or the stored input hashes / compiler
version no longer match. The check is identical everywhere; only *who runs it* differs:

- **Dev**: the game process checks staleness when an asset is first requested and recompiles
  inline (on the worker thread that is loading it) before loading. First run is slow, then it's cache hits.
- **Ship**: the CLI tool walks the source tree, compiles everything stale, and the resulting
  compiled tree *is* the shipped content folder. Shipped builds skip the staleness check entirely
  and never reference the compiler assemblies.

### Per-type mapping

| Type | Source | Compile step | Compiled body |
|---|---|---|---|
| Texture | png, jpg, ... | `CapriKit.SuperCompressed` encoder (mips baked in) | KTX2 |
| Shader | hlsl | DXC/FXC per `#pragma`-marked entry point | DXBC blob per entry point |
| Model | tbd (gltf?) | parse, triangulate, build interleaved buffers | vertex + index blobs |
| Audio | wav, ogg | tbd (likely near pass-through) | PCM or compressed blob |

Note that input-file tracking (an `.hlsl` including `common.hlsli`) is a *build* dependency,
not a runtime asset reference — it exists only so staleness and hot reload know that touching
`common.hlsli` dirties every shader that included it. The "flat assets" decision is untouched.

## Phase 2: Loading

### Identity and typed access

```csharp
public sealed record Asset<T>(string Path); // relative path, forward slashes, lower case

public static class GameAssets // game code declares what it uses
{
    public static readonly Asset<Texture2D> Grass = new("textures/grass.png");
    // snip
}
```

Per assumption 1, interpretation (sRGB vs linear, transcode target, ...) is supplied by the
caller **at request time** as an optional per-type options record; nothing is persisted.

### Bundles and background loading

This generalizes the pattern already proven in `CapriKit.Tests.Tool/Program.cs`:
`BackgroundWorker` jobs write results (or one exception) into a `LightweightChannel`,
and the main loop drains it once per frame.

```csharp
var bundle = assets.StartLoading([GameAssets.Grass, GameAssets.Tree /* snip */]);

// in the main loop, once per frame:
assets.Update();            // drains channels, publishes finished assets, applies hot reloads

if (bundle.IsLoaded)        // all jobs finished (faulted bundles rethrow here)
{
    AssetRef<Texture2D> grass = bundle.Get(GameAssets.Grass);
}
```

`bundle.Progress` (loaded/total) falls out for free for loading screens.

### Threading: answering "do I need two steps?" (brief 3b)

Mostly no, on D3D11. `ID3D11Device` is **free-threaded**: creating textures, buffers, and
shaders — including uploading initial data — is legal from any thread. Only the
**immediate `DeviceContext`** is single-threaded. Since mips are baked offline, nothing in
the current asset set needs the context at load time, so a worker thread can hand back a
fully GPU-resident texture.

The main-thread coordination point still exists, but it is just `assets.Update()` draining
the channel: finished objects become *visible* to game code only on the main thread, which is
also the only place the registry is ever mutated — no locks anywhere. If a future asset type
does need context work (e.g. runtime `GenerateMips`), it can queue an action that `Update()`
executes; that seam costs nothing today.

### Indirection for hot reload (3d)

The registry maps `Asset<T>` → `AssetRef<T>`, created once and permanent (assumption 2:
loaded stays loaded, so no lifetime/refcount management at all).

```csharp
public sealed class AssetRef<T>
{
    public T Value { get; }      // a field read, no lookup cost per use
    public int Version { get; }  // bumped on hot reload
    // snip: internal setter used by the registry during Update()
}
```

Game code holds the `AssetRef<T>` and reads `Value` when used. Derived objects (an input
layout built from a vertex shader blob) either poll `Version` or subscribe to a sparse
`Changed` event — needed rarely, mostly for shaders.

## Hot Reload (dev only)

```
FileSystemWatcher (source tree)
  → debounce (editors save in bursts)
    → look up which assets have that file as an input (from stored input lists)
      → recompile + reload on a worker, same code path as a normal load
        → assets.Update() swaps AssetRef.Value on the main thread, bumps Version
          → old object disposed (D3D11 keeps GPU resources alive while in flight, so this is safe)
```

Failures (syntax error in a shader) write the exception to the channel; `Update()` reports
it and keeps the old value live — a typo never kills the running game.

## Project Layout

| Project | Contents | Referenced by |
|---|---|---|
| `CapriKit.Assets` | `Asset<T>`, registry, bundles, loaders, hot-reload swap. Refs `DirectX11`, `SuperCompressed` (transcode), `Concurrency`, `IO`. | game, always |
| `CapriKit.Assets.Compilers` | `IAssetCompiler` implementations, staleness, watcher. Refs `SuperCompressed` (encode), shader compiler. | game in dev builds; CLI tool |
| `CapriKit.Assets.Tool` | thin CLI over Compilers for the shipping build step | build scripts |

Boilerplate budget per new asset type: **one compiler class + one loader class**
(MiniEngine3 needed processor + content wrapper + settings + serialization per type; the
shared container format and the no-settings/no-lifetime assumptions eliminate the rest).

## Deliberately Out of Scope

- Unloading / reference counting (assumption 2).
- Cross-asset references and cascading reloads (flat-assets decision).
- Per-asset metadata files (assumption 1).
- Packed archives — see appendix.

## Risks & Open Points

1. **Encoder settings vs assumption 1.** Normal maps and albedo textures ideally encode
   differently (UASTC vs ETC1S, linear vs perceptual metrics). If one default proves
   insufficient, a filename convention (`*_n.png`) is the escape hatch that doesn't violate
   "no metadata files".
2. **Model and audio source formats** are undecided; the architecture only assumes "some
   compiler produces blobs". Worth a separate research note before implementing those compilers.
3. **Input tracking in `CapriKit.IO`.** Compilers need a tracking read-only file system
   (MiniEngine3's `TrackingVirtualFileSystem` is the precedent); check what `CapriKit.IO`
   is missing.
4. **Native binary size.** The SuperCompressed native DLL contains encoder *and* transcoder;
   shipped games carry encoder code they never call. Acceptable for now, splittable later.

## Appendix: Packed Archives (not now, maybe later)

Skipped because loose files make hot reload, staleness, and debugging trivially simple, and
the classic motivations (seek latency, file-handle overhead) matter little on modern SSDs.
Remaining real benefits: fewer/smaller patch artifacts, whole-archive compression, and light
obfuscation.

The door stays open cheaply: loaders read compiled containers through
`IReadOnlyVirtualFileSystem`, so a future `PackedFileSystem` implementation (one archive =
one mounted file system) would slot in without touching any pipeline code. If it ever
happens, pack as a *post-step* over the compiled tree so the compile pipeline never knows.

## Remark: uploading textures from a worker thread

`UpdateSubresource`/`Map` need the immediate context (main thread), but they are only for
writing into a resource that *already exists* (`Default`/`Dynamic` usage). Asset textures
instead pass their pixels as initial data to `ID3D11Device::CreateTexture2D`, which is
free-threaded: one `SubresourceData` entry per subresource (mip × array slice), copied by
the driver *during* the create call. Use `ResourceUsage.Immutable` — it requires initial
data at creation, forbids later updates, and lets the driver optimize.

```csharp
var subresources = new SubresourceData[mipCount];
for (var mip = 0; mip < mipCount; mip++)
{
    // for BC formats: rowPitch = Math.Max(1, (mipWidth + 3) / 4) * bytesPerBlock
    subresources[mip] = new SubresourceData(pointerToMipData, rowPitch);
}
var desc = new Texture2DDescription(format, width, height, mipLevels: mipCount, /* snip */ usage: ResourceUsage.Immutable);
var texture = device.CreateTexture2D(desc, subresources);
// pointers only need to stay pinned until CreateTexture2D returns — the copy is synchronous
```

Caveats: free-threaded means thread-safe, not necessarily concurrent — without
`DriverConcurrentCreates` (see `CheckFeatureSupport(D3D11_FEATURE_THREADING)`) creates
serialize on a device-wide lock, which is still correct. And none of this holds if the
device were created with `D3D11_CREATE_DEVICE_SINGLETHREADED` (ours is not, see `Device.cs`).
