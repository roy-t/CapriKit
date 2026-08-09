# Asset Pipeline Rewrite Review

> Multi-agent review of the rewritten `source/CapriKit.AssetPipeline` and
> `source/CapriKit.AssetPipeline.DirectX11` (branch `feature/asset_pipeline`, commit `5a4a056`
> "Complete redo asset pipeline"). Four review agents ran in parallel — correctness,
> ease-of-use/DX/clarity, performance, and modern .NET constructs — each reading the full
> source plus its `CapriKit.IO` dependencies. Findings were deduplicated, re-ranked and
> cross-checked against a manual read; the blocking section was verified independently
> against the source before being written down.
>
> Supersedes `AssetPipelineReview.md`, which reviewed the *pre-rewrite* design.

**Headline: the code cannot currently run end-to-end.** `AssetManager`'s constructor throws
for exactly the configuration `AddAssetPipeline` builds. That is consistent with the rest of
the evidence — commit `5a4a056` deleted all five `CapriKit.Tests/AssetPipeline/*` files without
replacement, and nothing in the repo calls `AssetManager.Load`. Read this document as a review
of a design that has not executed yet, not of working code.

## Overview

| # | Cluster | Finding | Severity | Location |
|---|---------|---------|----------|----------|
| B1 | Blocking | `AssetManager` ctor always throws — `Watch(DirectoryPath.Empty)` is rejected by every filesystem | Critical | `HotReloadManager.cs:38` |
| B2 | Blocking | Every shader with an `#include` fails to build — include root resolved against process CWD | Critical | `VertexShaderTranscoder.cs:14` |
| B3 | Blocking | Assets are never disposed — the `Line` wrapper is cast to `IDisposable`, not the asset | Critical | `AssetCache.cs:79,87` |
| B4 | Blocking | `AssetManager.Dispose` never disposes `HotReloadManager`, leaking the OS watcher | High | `AssetManager.cs:117` |
| C1 | Threading | Threading model undecided: async continuations mutate main-thread state | High | `AssetManager.cs:110`, `AssetCache.cs:62`, `HotReloadManager.cs:51` |
| C2 | Correctness | `#include`d files never hot-reload — dependency keys and watcher events use different path shapes | High | `VertexShaderTranscoder.cs:13-14` |
| C3 | Correctness | A failed encode destroys the last good build artifact | Medium | `AssetEncoder.cs:22` |
| C4 | Correctness | Concurrent `Load` of the same id throws | Medium | `AssetManager.cs:24` |
| C5 | Correctness | `IsUpToDate` never checks the primary source; `<` should be `!=` | Medium | `AssetManager.cs:93,102` |
| C6 | Correctness | Successful hot reload logged at `LogLevel.Error` | Low | `HotReloadManager.cs:161` |
| C7 | Correctness | Leftover `v2` namespace from the rewrite | Low | `HotReloadManager.cs:8` |
| D1 | DX | `Load` returns an `IDisposable` the caller must never dispose | High | `AssetManager.cs:24` |
| D2 | DX | The reader-lifetime rule is a `//` comment, so it never reaches IntelliSense | High | `IAssetTranscoder.cs:28-30` |
| D3 | DX | `AssetId(string Key, FilePath Path)` — swapping the arguments compiles | Medium | `Asset.cs:10` |
| D4 | DX | Build artifacts land beside sources; `.cka` not in `.gitignore` | Medium | `AssetUtilities.cs:11` |
| D5 | DX | Transcoder + settings passed on every single `Load` | Medium | `AssetManager.cs:24` |
| D6 | DX | Documentation gaps and one misleading `<inheritdoc>` | Medium | `IAssetTranscoder.cs:6-16` |
| D7 | DX | Error messages: one bare `Exception`, one raw `KeyNotFoundException`, one silent blackout | Medium | `AssetCache.cs:27,56`, `AssetDecoder.cs:78` |
| P1 | Perf | Every load reads the whole file twice | High | `AssetManager.cs:35,38` |
| P2 | Perf | `ArrayPool<byte>.Shared` silently stops pooling above 1 MB → LOH churn | High | `AssetDecoder.cs:24,60` |
| P3 | Perf | No `ConfigureAwait(false)` anywhere; a 50–500 ms `D3DCompile` can land on the main thread | Medium | 12 awaits across 4 files |
| P4 | Perf | First-run path throws an exception as normal control flow | Medium | `AssetDecoder.cs:55` |
| P5 | Perf | Redundant `stat` syscalls, per-load `Guid.Parse`, per-load transcoder allocation | Low | `AssetDecoder.cs:17,54` |
| M1 | Modern | Constrain `TSettings : IEquatable<TSettings>`; `NoSettings` as `readonly record struct` | High | `IAssetTranscoder.cs:17`, `AssetManager.cs:81-90` |
| M2 | Modern | No `CancellationToken` anywhere, unlike the rest of `CapriKit.IO` | Medium | `AssetManager.cs:24`, `IAssetTranscoder.cs:26` |
| M3 | Modern | Assorted one-line modernisations, no downside | Low | various |
| M4 | Modern | Library is AOT/trim-clean but unguarded — `IsAotCompatible` set nowhere | Low | `Directory.Build.props` |

**Ground truth** (verified): `net10.0` (`net10.0-windows` for DirectX11), `LangVersion Latest` → C# 14,
`Nullable enable` with `WarningsAsErrors`, version `0.1.0-alpha`. No AOT/trim properties set anywhere
in the repo. Both projects compile with 0 warnings, so nothing below is flagged by the compiler.

---

## Blocking — prevents the pipeline running at all

### B1 · `AssetManager`'s constructor always throws

`HotReloadManager.cs:38`:

```csharp
Watcher = fileSystem.Watch(DirectoryPath.Empty); // Watch for all changed, usually fileSystem is a ScopedVirtualFileSystem
```

`DirectoryPath.Empty` is `new(null)` (`DirectoryPath.cs:12`), so `Path == ""` and
`IsAbsolute` is `Path.IsPathFullyQualified("")` → `false`.
`ReadOnlyScopedFileSystem.Watch` (`ScopedFileSystem.cs:143`) begins with
`ThrowIfPathIsOutsideBasePath(directory)`, which `Debug.Assert(path.IsAbsolute)` fires on in
Debug builds and then throws `ForbiddenPathException` because `""` does not start with the base
path. A plain `FileSystem` is no better: `Path.GetFullPath("")` throws `ArgumentException`.

`AssetManager` constructs a `HotReloadManager` unconditionally (`AssetManager.cs:21`), and
`AddAssetPipeline` (`ServiceCollectionExtensions.cs:15`) supplies exactly a
`new FileSystem().ScopedTo(assetDirectory)`. So the documented entry point throws before you can
load anything.

The intent is already stated in the comment on that line, and the overload exists:

```csharp
Watcher = fileSystem.Watch(); // ScopedFileSystem.cs:151 — watches BasePath, no path check
```

It currently lives only on `ReadOnlyScopedFileSystem`, so it needs promoting to
`IReadOnlyVirtualFileSystem` to be reachable through the interface. The alternative is to make
`ThrowIfPathIsOutsideBasePath` treat an empty relative path as "the base path itself".

### B2 · Every shader with an `#include` fails to build

`VertexShaderTranscoder.cs:11-15`:

```csharp
var source = await fileSystem.ReadAllText(id.Path);
var includePath = id.Path.Directory;   // relative, e.g. "shaders/"
var bytes = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, id.Key, id.ToString());
```

`includePath` is relative. `ShaderIncludeResolver`'s constructor wraps it in a
`ReadOnlyScopedFileSystem`, whose constructor does `BasePath = basePath.ToAbsolute()`
(`ScopedFileSystem.cs:49`) → `Path.GetFullPath("shaders/")`, resolved against
**`Environment.CurrentDirectory`**.

With assets at `C:/game/content` and the process running from `C:/game/bin`, the resolver builds
`C:/game/bin/shaders/foo.hlsl` and hands it to the underlying scoped filesystem, which rejects it
with `ForbiddenPathException`. Shaders only compile when CWD happens to equal the asset root.

Fix: resolve the include directory against the filesystem's base rather than the CWD — pass the
already-scoped filesystem plus the relative directory and let `ShaderIncludeResolver` keep paths
relative instead of calling `ToAbsolute()`.

### B3 · Assets are never disposed

`AssetCache.cs:76-80` and `:83-90`:

```csharp
Lines.Remove(key, out var entry);
(entry as IDisposable)?.Dispose();   // entry is Line, not entry.asset
```

`Line` (`AssetCache.cs:11-15`) does not implement `IDisposable`, so the `as` cast is *always* null
and the `?.` is always a no-op. The same defect is in `Dispose()`, where `value` is also a `Line`.

Load a shader, unload it, call `Update()`: the entry leaves the dictionary and the `VertexShader`
— along with its `ID3D11VertexShader` — is silently dropped. Every collected asset leaks its
native handle, `AssetManager.Dispose()` leaks all live ones, and D3D11 will report live-object
warnings on device release.

```csharp
(entry?.asset as IDisposable)?.Dispose();
(value.asset as IDisposable)?.Dispose();
```

The naming inside `Line` is what hides this — PascalCase primary-constructor parameters shadowing
camelCase public fields, five lines apart. Renaming the type to `Entry` and the field to `Asset`
makes the bug visible at a glance.

### B4 · `AssetManager.Dispose` never disposes `HotReloadManager`

`AssetManager.cs:117-120` disposes only `Cache`. `HotReloadManager.Dispose` is the only caller of
`Watcher.Stop()` (`HotReloadManager.cs:148`), and grep confirms nothing in the repo invokes it.
After disposal the `FileSystemWatcher` keeps running, keeps enqueueing into
`FileSystemEventQueue`, and keeps the whole manager graph — including disposed D3D wrappers —
alive. Creating and disposing several `AssetManager`s leaks one OS watcher handle each.

---

## Correctness

### C1 · The threading model is undecided — and that is the root defect

Three separate findings share one cause. `AssetManager.Load` is `async`; with no
`SynchronizationContext` (normal for a game loop) the continuation after
`await AssetDecoder.Decode(...)` runs on a thread-pool thread. `RegisterAsset`
(`AssetManager.cs:110-115`) then mutates main-thread state from there:

- **`AssetCache.Collect`/`Dispose` take no lock** (`AssetCache.cs:62`, `:83`) while `Put`, `TryLease`
  and `Return` all do. `Lines.Add` from a loader thread during `Collect`'s `foreach` gives
  `InvalidOperationException: Collection was modified`, or a read racing a dictionary resize.
- **Zero-refcount resurrection.** `Collect` snapshots a `refCount == 0` entry into `toCollect`
  (`:64-72`); before the removal loop at `:76` a background `TryLease` can bump it to 1 and hand the
  asset to a caller. `Collect` then removes and (once B3 is fixed) disposes it. The caller holds a
  disposed asset, and its later `Unload` throws `KeyNotFoundException` at `:56`.
- **`HotReloadManager` has no synchronisation at all.** `Track` (`:51-68`) writes the plain
  `Dictionary` fields `Tracked` and `Dependents` while `DrainFileChanges` (`:91`) and `ReloadOne`
  (`:114`) read them on the main thread.

The class doc at `AssetCache.cs:6` also contradicts itself: "Assets can be leased and returned at
any time (though the class requires single-threaded access)" describes a class that nonetheless
takes a `Lock`.

**Cheapest coherent fix:** have `RegisterAsset` enqueue onto a `ConcurrentQueue` that `Update()`
drains on the main thread, mirroring how `PendingReloads` already works. The cache then genuinely
*is* main-thread-only, the `Lock` can be deleted rather than extended, and the doc comment becomes
true. Decide this before touching anything else in `AssetCache` — it determines whether the lock
stays at all.

### C2 · `#include`d files never hot-reload

The transcoder reads its primary file straight off the spy
(`fileSystem.ReadAllText(id.Path)`), so the spy records a **relative** path. Includes go through
`new ReadOnlyScopedFileSystem(spy, includePath)`, whose `OpenRead` calls
`Source.OpenRead(GetFilePath(file))` — resolving to **absolute** before the spy ever sees it.
Observed in an agent's repro:

```
spy recorded: shaders/a.hlsl
spy recorded: C:/Users/.../capri_repro_assets/shaders/a.hlsl
```

Meanwhile `ScopedFileSystemEventListener.cs:22` normalises every watcher event to
`e.File.GetPathRelativeTo(BasePath)` — always relative. So `DrainFileChanges`
(`HotReloadManager.cs:91`) looks up a relative key in a `Dependents` map keyed absolutely, and
misses.

Edit a top-level `.hlsl` and it hot-reloads; edit an `#include`d one and nothing happens, ever.
`IsUpToDate` still works for these files because `ScopedFileSystem.Exists` accepts both shapes —
which is precisely why this would be easy to ship unnoticed.

Fix: normalise dependency keys to one representation. Simplest is to have
`VirtualFileSystemSpy` record `path.GetPathRelativeTo(basePath)`.

### C3 · A failed encode destroys the last good build artifact

`AssetEncoder.cs:22` opens the `.cka` with `FileMode.Create` (truncate) *before*
`encoder.Encode` runs. A shader typo during hot reload throws out of `WritePayload` and leaves a
0-byte `.cka`. It is recovered on the next run — `TryDecodeBuildMetaData`'s blanket `catch`
(`AssetDecoder.cs:78`) returns null and forces a rebuild — so this is not silent corruption, but
the previously-good artifact is gone. Encode into a buffer first and open the output file only
once encoding succeeded. This is also a prerequisite for cancellation being safe (see M2).

### C4 · Concurrent `Load` of the same `AssetId` throws

There is no in-flight de-duplication. Two overlapping calls both miss `TryLease`, both call
`AssetEncoder.Encode` on the same output path (the second `CreateReadWrite` throws `IOException`),
and if they get past that, the second `Cache.Put` throws
`new Exception($"Cache already contains asset: {id}.")` (`AssetCache.cs:27`). The same collision
exists between a `Load` and a concurrent `HotReloadable.Reload` of the same asset.
Fix: a `Dictionary<AssetId, Task<TAsset>>` of in-flight loads.

### C5 · `IsUpToDate` never checks the primary source, and compares timestamps with `<`

`AssetManager.cs:93-105` iterates only `build.Dependencies`, which is whatever the transcoder
happened to open *through the spy*. `AssetEncoder.cs:19`'s `ThrowOnFileNotFound(id.Path, fileSystem)`
uses the raw filesystem, and `Exists` is not spied, so nothing guarantees `id.Path` appears in the
list. A transcoder that generates content or reads its source by another route produces an empty
dependency list — the `foreach` body never runs, `IsUpToDate` returns `true` unconditionally, and
the stale artifact is used forever. Always check `id.Path` explicitly, and/or seed the spy with it.

Separately, `:102` uses `if (version < lastWrite)`. Restoring an older copy of a source file with
its mtime preserved (backup restore, `robocopy`, some VCS tooling) leaves `lastWrite <= version`,
so no rebuild happens and the wrong asset is used. `version != lastWrite` is the standard
formulation.

### C6 · Successful reload logged at `Error`

`HotReloadManager.cs:161` — `[LoggerMessage(Level = LogLevel.Error, Message = "Reloading asset completed: {asset}")]`,
copy-pasted from the adjacent `LogReloadFailed`. Every successful hot reload is reported as an
error to the log sink and to anything filtering on error level.

### C7 · Leftover `v2` namespace

`HotReloadManager.cs:8` still declares `namespace CapriKit.AssetPipeline.v2;` while every other
file moved to `CapriKit.AssetPipeline`; `AssetManager.cs:1`'s `using CapriKit.AssetPipeline.v2;`
exists purely to compensate. `git show --stat 5a4a056` shows `v2/` → `` renames for `AssetCache`
and `HotReloadable` but not for this file. A `v2` sub-namespace is visible to anyone typing
`CapriKit.AssetPipeline.` in IntelliSense.

### Checked and found clean

Worth recording, so these are not re-reviewed later:

- **Encode/decode round trip.** Every field traced. `AssetEncoder` writes
  `Guid, int | int len, settings | int len, payload | int count, (long ticks, string path)*` and
  `AssetDecoder` reads exactly that, in that order, matching the format comment at
  `AssetEncoder.cs:9`. Endianness matches (`BinaryPrimitives` LE on both sides), `Guid` uses
  `bigEndian: false` both ways, `Write(FilePath)` binds to the `string` overload via the implicit
  conversion and pairs correctly with `ReadString`'s 7-bit-length prefix, and `SliceUnread`
  correctly advances the outer reader past each length-delimited section. No mismatch found.
- `build != default` (`AssetManager.cs:36`) does behave as a null check — the record-synthesized
  `op_Inequality` handles null correctly. (Still worth changing for clarity; see M3.)
- The watcher **is** debounced (`HotReloadManager.cs:78-82`, 0.5 s of quiet), and the `.cka` is
  written on the raw filesystem *before* the spy is created (`AssetEncoder.cs:22` vs `:24`), so
  build artifacts are not recorded as their own dependencies. No infinite rebuild loop.
- `IVertexShader.HotSwap` (`VertexShader.cs:12-19`) correctly preserves the live object's identity
  and disposes only the orphaned old `ID3D11VertexShader`, so existing holders stay valid.
- `AssetDecoder`'s `ArrayPool` rent/return is balanced in `finally` and the sequence is bounded to
  the real length rather than the rented length.

---

## Ease of use, DX and clarity

### D1 · `Load` hands back an `IDisposable` the caller must never dispose

`AssetManager.cs:24` returns a bare `TAsset`. For the only worked example that is `IVertexShader`,
which is `IDisposable`. The C# reflex on an `IDisposable` returned from a method is `using` —
which destroys an asset the cache still hands to every other caller, while the refcount never
notices. `Unload(id)` is the real release, and nothing in the type system, the name, or the docs
says so: `Load` and `Unload` have no XML documentation at all.

The failure is symmetric. Over-unloading is silent — `AssetCache.Return` (`:52-59`) decrements past
zero without complaint and `Collect` then frees a live asset. Forgetting to unload leaks with no
diagnostic.

**Small fix** — document the ownership contract and make `Return` refuse to go negative:

```csharp
/// <summary>
/// Loads an asset, building it first if there is no up-to-date build artifact.
/// The returned instance is <b>owned by the AssetManager</b> and shared with every other
/// caller of the same <paramref name="id"/>: never dispose it, and never keep it past the
/// matching <see cref="Unload"/>. Each successful Load takes one reference; call
/// <see cref="Unload"/> exactly once per Load.
/// </summary>
```

```csharp
// AssetCache.Return
if (!Lines.TryGetValue(id, out var entry))
    throw new InvalidOperationException($"Cannot unload asset {id}: it was never loaded, or it was already unloaded once per Load.");
if (--entry.refCount < 0)
    throw new InvalidOperationException($"Asset {id} was unloaded more often than it was loaded.");
```

**Medium fix**, matching the `AssetRef<T>` idea in `AssetPipelineArchitecture.md`: return an
`AssetLease<TAsset> : IDisposable` with a `.Value`, so `using` becomes the *correct* thing rather
than the destructive one. Costs one struct plus `.Value` at every use site.

### D2 · The most dangerous rule in the contract is in a `//` comment

`IAssetTranscoder.cs:28-30` states that the reader's buffer is only valid for the duration of the
call and decoders must copy out anything they keep. That is true — `AssetDecoder.cs:24,43` rents
from `ArrayPool` and returns it in a `finally` — and it is invisible to a package consumer,
because `//` comments do not ship in the XML documentation file or IntelliSense. A decoder that
retains a `ReadOnlySequence` slice compiles fine and later reads recycled pool memory under load.

Promote it to `<remarks>` on `Decode`, along with the async rationale at `:20`:

```csharp
/// <remarks>
/// The reader is backed by a pooled buffer that is recycled the moment this method returns:
/// copy out (<c>ToArray</c>, <c>CopyTo</c>) anything you keep. Storing the reader, a slice of
/// it, or a span into it will silently read another asset's bytes later.
/// Runs synchronously, possibly on a worker thread; do not touch main-thread-only state.
/// </remarks>
```

### D3 · `AssetId(string Key, FilePath Path)` — swapping the arguments compiles

`Asset.cs:10`. `FilePath` has `implicit operator FilePath(string?)` (`FilePath.cs:104`), so
`new AssetId("shaders/basic.hlsl", "VsMain")` — path first, the order every reader will guess —
compiles cleanly and fails at runtime with `FileNotFoundException: ... VsMain`. There is also no
way to express "no sub-resource" other than `new AssetId("", path)`.

```csharp
/// <param name="Path">Virtual path of the source file the asset is built from.</param>
/// <param name="Key">Names a sub-resource inside <paramref name="Path"/> — e.g. the HLSL entry
/// point for a shader. Empty means "the whole file". Assets with the same Path but different
/// Keys are separate assets with separate build artifacts.</param>
public sealed record AssetId(FilePath Path, string Key = "");
```

There are zero call sites today, so this is free now and will not be later.

### D4 · Build artifacts land beside the sources

`AssetUtilities.cs:7-16` maps `grass.png` → `grass.png.cka` **in the same directory**, and `.cka`
is not in `.gitignore`. `AssetPipelineArchitecture.md` specifies a mirrored compiled tree,
deliberately separate. First-run experience is that the user's art folder fills with `.cka` files
and their next `git status` is noise.

Small: add `*.cka` to `.gitignore` and hoist the extension to a documented
`public const string BuildArtifactExtension`. Real fix: a `DirectoryPath outputDirectory` on the
`AssetManager` constructor and `AddAssetPipeline`, with `ToEncodedFilePath` rebasing onto it
(~15 lines, and it is the design already written down).

### D5 · Transcoder and settings on every `Load`

`AssetManager.cs:24` requires two extra arguments per call, both constant for the lifetime of the
program. `AssetManagerExtensions.cs:9-13` shows what people reach for and why it is not enough:

```csharp
public static Task<IVertexShader> LoadVertexShader(this AssetManager assetManager, Device device, AssetId id)
{
    var transcoder = new VertexShaderTranscoder(device);   // new instance per call
    return assetManager.Load(id, transcoder, default);
}
```

A fresh transcoder per call means `HotReloadManager.Track` stores a distinct transcoder object per
asset, and `device` is threaded through every call site forever.

Recommended small fix — bind transcoder and settings once, keep full compile-time typing, add no
registry and no "missing transcoder" runtime failure:

```csharp
/// <summary>How to build and load one kind of asset. Create once, reuse for every Load.</summary>
public sealed record AssetSource<TAsset, TSettings>(IAssetTranscoder<TAsset, TSettings> Transcoder, TSettings Settings)
    where TAsset : class;

public Task<TAsset> Load<TAsset, TSettings>(AssetId id, AssetSource<TAsset, TSettings> source) where TAsset : class
    => Load(id, source.Transcoder, source.Settings);
```

Alternatives and their cost: *registration* (`assets.Register(...)` + `Load<IVertexShader>(id)`)
reads best at the call site but reintroduces the `TranscoderCollection` deleted in `5a4a056`,
trades a compile error for a runtime "no transcoder registered", and forces settings to be
per-type rather than per-load. *DI* is worse here, because the transcoder needs the `Device` and
the container would have to know about graphics. A typed `AssetHandle<T>` bundling id + transcoder
+ settings is the nicest end state but wants a per-`Device` catalog object.

### D6 · Documentation

Measured against the standard in `CLAUDE.md` ("teach the library user how to use this correctly
and when not to").

**Undocumented public members:** `AssetManager` itself — the entry point of the package — plus its
constructor, `Load`, `Unload` and `Dispose`; `IAssetTranscoder.Id` and `.Version`; `NoSettings`;
all four abstract members of `NoSettingsTranscoder`; `AddAssetPipeline`;
`AssetManagerExtensions.LoadVertexShader`.

`Id`/`Version` is the highest-value gap in the file: they are the cache-invalidation keys, and
nothing tells an implementer that `Version` must be bumped whenever `Encode`'s output format
changes — otherwise every user's stale artifacts are fed to the new decoder.

**Misleading docs:**

- `IAssetTranscoder.cs:6-9` — "Interface for classes that builds assets ... and load them when the
  program needs them" describes `AssetManager`, not a transcoder. `:16`'s
  `<inheritdoc cref="IAssetTranscoder"/>` then copies that wrong sentence onto the generic
  interface people actually implement. Better: "One transcoder owns both halves of a single asset
  type: the offline build (`Encode`, dev machine) and the runtime load (`Decode`, game process)."
- `:33` — "Decodes the file created the `Encode`" is missing a word.
- `:39-50` — `WriteSettings` and `ReadSettings` have near-identical text, and `ReadSettings` says
  it "Decodes the settings ... **into** the stream" when it reads *from*. Neither mentions that
  `WriteSettings` doubles as the staleness comparison (see M1).
- `AssetCache.cs:6` — claims single-threaded access while the class locks. The truth is narrower
  and more useful: lease/return are thread-safe; `Collect` and `Dispose` are main-thread only.
- `AssetManager.cs:64` — `Update` has a `<remarks>` but no `<summary>`, so IntelliSense shows an
  empty description with a floating remark. It never says what `Update` does or what breaks if you
  skip it (nothing is ever freed; hot reload silently does nothing). Consider a
  `[Conditional("DEBUG")]` main-thread assertion so "must" is enforced rather than hoped for.

### D7 · Failure modes surfaced to the user

| Situation | Today | Verdict |
|---|---|---|
| Source file missing | `FileNotFoundException` naming the path — `AssetManager.cs:48` | Good |
| Build artifact missing at decode | `FileNotFoundException` naming file and asset — `AssetDecoder.cs:19` | Good |
| Artifact built by another transcoder | `InvalidDataException` naming both GUIDs and versions — `AssetDecoder.cs:132` | Good |
| `ThrowOnFileNotFound` in the encoder | `new FileNotFoundException(null, path)` — `AssetUtilities.cs:22` | Bad: `null` message, no asset context |
| Same id loaded twice concurrently | `throw new Exception(...)` — `AssetCache.cs:27` | Bad: uncatchable by type |
| `Unload` of an id never loaded | `KeyNotFoundException` — `AssetCache.cs:56` | Worst: names neither the asset nor the API |
| Same id loaded as two different types | `InvalidCastException`, no id — `AssetCache.cs:44` | Bad |
| **Corrupt build artifact** | `catch { return null; }` — `AssetDecoder.cs:78` | Worst for diagnosis |

That last row is the answer to "what does it say when an artifact is corrupt": *nothing*. It
silently rebuilds, which is the right recovery, but a permission error, a >2 GB
`OverflowException` (`AssetDecoder.cs:59`) and a genuine bug in someone's `ReadSettings` all look
identical and produce a rebuild on **every single load**, forever, with zero log output —
`TryDecodeBuildMetaData` has no `ILogger`. Pass the logger in and log once at Debug/Warning.

### Naming

- `AssetCache.Line` — "line" is a CPU-cache term; this is an entry, and the local variable is
  literally called `entry`. Renaming to `Entry` (and the field to `Asset`) is what exposes B3.
- `Asset<TAsset, TSettings>` — a type named `Asset`, with a type parameter named `TAsset`, and a
  member `Value` of that type. `LoadedAsset<T>` reads better; it is internal, so this is cheap.
- `HotSwap(TAsset instance, TAsset newParts)` — `newParts` is not parts, it is a fully constructed
  replacement. `HotSwap(TAsset live, TAsset replacement)`, and the doc should state who disposes
  the leftover wrapper: `VertexShader.cs:12-19` disposes the old `ID3D11VertexShader` but not the
  `newParts` wrapper, and that asymmetry needs saying.
- **"Transcoder" already means something else in this repo.** `CapriKit.SuperCompressed` has
  `Ktx2Transcoder`, using *transcode* in its Basis-Universal sense: compressed container → GPU
  format, at runtime. A future `TextureTranscoder : IAssetTranscoder<ITexture, TextureSettings>`
  will call `Ktx2Transcoder.Transcode(...)` inside its `Decode` — two unrelated meanings of the
  same word in one file. `AssetPipelineArchitecture.md` names these **Compilers** and **Loaders**.
  The one-interface-does-both design is genuinely better than MonoGame's four-piece split and is
  worth keeping; only the name collides. Suggest taking the doc fix now and revisiting the name
  when the texture transcoder lands and the collision becomes concrete.

### Ceremony cost in DirectX11 — proportionate

| File | Lines | Real statements |
|---|---|---|
| `VertexShaderTranscoder.cs` | 29 | 6 |
| `ShaderTranscoder.cs` | 26 | 8 |
| `AssetManagerExtensions.cs` | 14 | 2 |
| **Total per asset type** | **69** | **16** |

`NoSettingsTranscoder` is doing its job — roughly 4:1 lines-to-logic for a binary-serialized GPU
resource is fine, and there is no per-type "content writer + content reader + settings class" tax
like MonoGame's. The only remaining pure forwarding is `HotSwap` (one line delegating to the
asset's own `HotSwap`); an optional `HotSwappableTranscoder<TAsset>` base would remove 4 lines per
transcoder but is not worth it until there are three or more.

---

## Performance

Judged against two standards: the **encode** path runs rarely and offline-ish, so allocation
matters little; the **load** path runs during gameplay or level load, where allocations, copies
and main-thread stalls are expensive. `Update()` runs on the main thread every frame.

### P1 · Every load reads the whole file twice

`AssetManager.cs:35` calls `TryDecodeBuildMetaData`, which `ReadExactlyAsync`s the *entire* file —
payload included — just to `SkipPayload` (`AssetDecoder.cs:69`) and reach the dependency list at
the tail. Then `:38` calls `Decode`, which re-opens and re-reads the identical bytes.

Cost: 2× file bytes, 2× `FileStream` open, 2× buffer rent, per asset, on the common path. A 4 MB
texture costs 8 MB of IO. File open on Windows is ~50–500 µs cold, so the duplicate opens alone
are ~50–500 ms per 1000 assets.

Fix: one pass. Read the file once into a pooled buffer, parse header → settings → *retain the
payload slice* → skip to dependencies → run the up-to-date check → and only then call
`transcoder.Decode` on the retained slice. `SliceUnread` already gives exactly the slice needed;
the file format does not change. **Biggest single item in this review.**

### P2 · `ArrayPool<byte>.Shared` silently stops pooling above 1 MB

`AssetDecoder.cs:24,60`. `ArrayPool<T>.Shared` caps at `MaxArrayLength = 1024*1024`; above that
`Rent` calls `GC.AllocateUninitializedArray` and `Return` drops the array on the floor. So for any
asset over 1 MB — every texture and mesh — this is a plain **LOH allocation of the full file size,
twice per load** (see P1), each becoming garbage immediately. A 16 MB texture is 32 MB of LOH
garbage per load; 100 such assets is 3.2 GB of churn and forced gen2/LOH pressure during level
load, exactly the main-thread stall case that matters.

Fix: a dedicated `ArrayPool<byte>.Create(maxArrayLength: 64MB, maxArraysPerBucket: 2)` held static
on `AssetDecoder`, or a single reusable staging buffer on `AssetManager` (loads are already
serialised through the same manager). Fixing P1 first halves this for free.

### P3 · No `ConfigureAwait(false)` anywhere

12 awaits across `AssetManager.cs`, `AssetDecoder.cs`, `AssetEncoder.cs`, `HotReloadable.cs`.
`CapriKit.IO` uses it on all 6 of its awaits, so the pipeline is inconsistent with its own
dependency. If a host installs a `SynchronizationContext` — an editor or tooling host, an
ImGui-driven tool, a test harness — every continuation posts back to the main thread. The worst
case is concrete: `VertexShaderTranscoder.cs:13-15` awaits `ReadAllText` and then runs a
synchronous `D3DCompile` of **50–500 ms** on the continuation.

Add `.ConfigureAwait(false)` throughout and enable CA2007 so it stays enforced. Note this makes
C1's race explicit rather than worse — settle the threading model first, and do not treat adding
it as closing the threading question.

Related: `Load` runs synchronously on the caller's thread up to the first real yield, including
the blocking `FileStream` open. Either document that `Load` must be called off the main thread, or
open the file on a worker.

### P4 · The first-run path throws as normal control flow

`AssetDecoder.cs:55` throws `FileNotFoundException` when the artifact does not exist yet, caught by
the blanket handler at `:78`. "Asset has never been built" is the expected first-run state, not an
error. Undebugged that is ~5–50 µs; **under a debugger, first-chance exception notifications cost
1–10 ms each**, so a fresh build of 1000 assets becomes 1–10 seconds of pure debugger stall and
floods the exception window. One line: `if (!fileSystem.Exists(inputPath)) { return null; }` before
the `try`. Cheapest high-value fix in this section.

### P5 · Smaller items

- **~6 redundant `stat` syscalls per load.** `AssetDecoder.cs:17,54` call `Exists`, then
  `OpenRead` → `FindOrThrow` → `GetFileInfo().Exists`, then the actual open — and all of it twice
  because of P1. `FileMode.Open` already throws `FileNotFoundException`, so `FindOrThrow` is pure
  duplication. ~30–90 ms per 1000-asset level load; the fix is *fewer* lines of code.
- **`Guid.Parse` on every transcoder construction** (`VertexShaderTranscoder.cs:9`) combined with a
  new transcoder per `Load` (`AssetManagerExtensions.cs:11`). `static readonly Guid` and a cached
  per-`Device` transcoder.
- **`IsUpToDate`'s two `ArrayBufferWriter`s** are ~64 bytes per load for `NoSettings` and ~600
  bytes for a realistic settings struct — real but noise next to P1/P2. The *structural* waste
  matters more: `build.Settings` was just deserialised purely so it could be re-serialised and
  compared byte-for-byte. See M1.
- **`ArrayBufferWriter` growth in `AssetEncoder`** (`:53`) doubles and discards, never using
  `ArrayPool`; combined with `PipeWriter` buffering everything until `FlushAsync` at `:31`, peak
  memory is ~3× payload size. Normally an encode-path shrug, except this runs during hot reload
  while the game is live, so the LOH churn causes gen2 pauses mid-play. Dropping `PipeWriter` for a
  single size-hinted `ArrayBufferWriter` + `output.WriteAsync` is simpler *and* faster.

### Explicitly judged not worth it

- **`Collect()`'s O(n) scan.** Genuinely zero-allocation in steady state (struct enumerator;
  `toCollect` only allocates when there is something to collect). ~5–20 µs/frame at 1000 assets
  ≈ 0.1% of a 16.6 ms budget. Restructuring to a candidate list pushed from `Return()` is worth
  doing for *clarity*, and the O() improvement is a bonus — not the reason.
- **`ValueTask<TAsset>` for `Load`.** ~72 bytes per load against a public signature change and
  `.AsTask()` at any call site that stores the result. If synchronous cache hits matter, the honest
  fix is a genuinely synchronous `TryGetLoaded(id, out asset)`, which needs no `ValueTask`.
- **Lock contention in `AssetCache`.** Uncontended `Lock` is ~20 ns and `Load` is not per-frame.
  Do not remove the lock for speed (C1 may remove it for a better reason).
- **`Stopwatch.GetElapsedTime` every frame** (`HotReloadManager.cs:78`) — ~20–25 ns. Negligible.

### Benchmarks

`CapriKit.Benchmarks` uses `BenchmarkSwitcher.FromAssembly`, so a `[MemoryDiagnoser]` class drops
straight in — but it currently references only `CapriKit.Concurrency` and `CapriKit.IO`, so a
project reference is needed. Worth measuring, against `InMemoryFileSystem` to isolate CPU from
disk: `Load` on the cold-but-up-to-date path (the direct before/after for P1 and P5),
`AssetCache.Collect()` vs entry count at 100/1,000/10,000 (the one estimate above derived from
first principles rather than measurement), and `AssetEncoder.Encode` peak allocation across
payload sizes. **Fix B1 first** — none of this is currently exercisable end-to-end.

---

## Modern .NET

Targeting `net10.0` / C# 14. House style already in use elsewhere in the repo, so these are safe:
`SearchValues` (`IOUtilities.cs:11-12`), `ValueTask` and optional `CancellationToken` on every
async IO helper (`VirtualFileSystemExtensions.cs:18-63`), `readonly record struct`, primary
constructors, collection expressions, `[LoggerMessage]`. Never used anywhere in the repo:
static abstract interface members, `FrozenDictionary`, `TimeProvider`, `CollectionsMarshal`,
generic math. `System.Threading.Lock` appears exactly once — in `AssetCache.cs:17`, so the code
under review is already the most modern locking in the repo.

### M1 · `where TSettings : IEquatable<TSettings>` + `readonly record struct NoSettings`

`AssetManager.cs:81-90` decides staleness by serializing *both* settings objects into fresh
`ArrayBufferWriter<byte>`s and comparing spans. Nothing in `IAssetTranscoder.cs:39-43` tells the
implementer that their serializer must be **deterministic** — iterate a `Dictionary`, write a
`float` through a culture-sensitive path, or include a timestamp, and every `Load` decides the
artifact is stale and rebuilds forever, with no error.

**The agents disagreed here, and the disagreement is instructive.** One proposed
`EqualityComparer<TSettings>.Default.Equals(...)`; another argued *against* it, because it silently
degrades to reference equality for a settings type that forgets to implement equality — a
"rebuild forever" cliff that fails silently, i.e. exactly the bug it was meant to remove. Adding
the constraint answers that objection directly:

```csharp
public interface IAssetTranscoder<TAsset, TSettings>
    where TAsset : class
    where TSettings : IEquatable<TSettings>

// AssetManager.IsUpToDate
if (!settings.Equals(build.Settings)) { return false; }
```

and `public readonly record struct NoSettings;` gets `IEquatable<NoSettings>`, `Equals`,
`GetHashCode` and `==` from one word. Nine lines become one, two allocations and two serializations
leave every load, and the hidden determinism contract disappears rather than needing to be
documented. Source-breaking — which at `0.1.0-alpha`, with `NoSettings` as the only settings type
in the repo, is the right moment.

### M2 · `CancellationToken` — a real gap, with a caveat

No method in the pipeline takes a token, though `CapriKit.IO`'s async helpers all accept one and
`AssetDecoder.cs:28,64` calls `ReadExactlyAsync` without one. Add
`CancellationToken token = default` as the trailing parameter and thread it into
`IAssetTranscoder.Encode`, which is where the seconds actually go.

Honest caveat: this buys nothing on its own. `ShaderCompiler.CompileVertexShader` is a synchronous
blocking call inside an `async` method, so a token only takes effect at the cheap IO awaits — and
cancelling mid-`Encode` leaves a truncated `.cka` (C3). Add the parameter, but treat the
"cancel the loading screen" story as unfinished until transcoders poll and the write is atomic.
Source-breaking for `IAssetTranscoder.Encode`, not for `AssetManager.Load`.

### M3 · One-line modernisations with no downside

- `build is not null` instead of `build != default` (`AssetManager.cs:36`) — `!= default` on a
  record dispatches through the synthesized `op_Inequality` instead of a plain null test.
- `catch (Exception ex) when (ex is not OperationCanceledException)` at `AssetDecoder.cs:78` — the
  bare `catch` today also eats `OutOfMemoryException`, and once M2 lands it would convert "the user
  cancelled" into "the metadata was unreadable, rebuild from scratch".
- `toCollect ??= []` (`AssetCache.cs:69`).
- Seal the records (`Asset.cs:10,12,15`) — `AssetId` is a `Dictionary` key in three places, so
  sealing removes the `EqualityContract` virtual call from every lookup and makes the classic
  derived-record-never-equals-base cache miss impossible. `record class` is also a redundant
  spelling of `record`.
- `[LoggerMessage]`: drop `static` and the `ILogger` parameter (the generator resolves the field on
  the containing type; both classes have one), removing a repeated argument from 13 call sites.
  PascalCase the placeholders per CA1727. And `AssetManager.cs:122-128` logs *every* asset load at
  `Information` — `Debug` fits a level load of a few thousand assets better.
- `static readonly Guid` instead of `Guid.Parse` per construction
  (`VertexShaderTranscoder.cs:9`).
- `PendingRebuilds.First()` (`HotReloadManager.cs:107`) boxes `HashSet<T>.Enumerator` through
  `IEnumerable<T>`. A `Queue<AssetId>` alongside the `HashSet` gives dedup *and* deterministic
  reload order instead of hash order.
- `ObjectDisposedException.ThrowIf(disposed, this)` on `Load`/`Update`/`Put`/`TryLease` — house
  style already in `CapriKit.SuperCompressed`. Today, using an `AssetManager` after `Dispose`
  silently succeeds against an emptied cache.
- Drop the redundant `public` on `IAssetTranscoder.cs:26,36,43,50` (`:59` already omits it).
- `FileChances` → `FileChanges` (`HotReloadManager.cs:22,39,89`).

### M4 · AOT/trim: clean today, unguarded tomorrow

No reflection, no `Activator.CreateInstance`, no `MakeGenericType`, no `dynamic`, no generic
*virtual* methods; `ServiceCollectionExtensions.cs:12` uses the lambda-factory `AddSingleton`
overload rather than reflection-based activation. **The library is AOT/trim-clean as written.**
`<IsAotCompatible>true</IsAotCompatible>` is set nowhere in the repo — adding it turns on the
analysers so the property stays true when a future transcoder reaches for reflection. One line, no
code change, and it is the kind of guarantee a game library gets asked for.

### Considered and rejected

- **`System.IO.Hashing` / `XxHash3` for freshness.** Content hashing means *reading every
  dependency file* on every load — strictly more IO on the path you most want fast. The one place
  it would pay is hot reload: an editor that rewrites a file unchanged currently triggers a full
  rebuild, and hashing only the file that raised the event would skip that. Worth doing *if*
  spurious rebuilds annoy you in practice. Note `System.IO.Hashing` is declared at
  `Directory.Packages.props:20` but referenced by no project — a dead central-package entry.
- **`FrozenDictionary`.** Every map here is write-often (`Lines`, `Tracked`, `Dependents`). Frozen
  collections are for build-once-read-forever. No fit.
- **Static abstract interface members for `Id`/`Version`.** Would make version drift a
  compile-time fact, but transcoders are *instances* carrying state (`VertexShaderTranscoder` holds
  a `Device`), so every method in `AssetEncoder`/`AssetDecoder`/`AssetManager` would need an extra
  `TTranscoder` type parameter — four files churned for harder-to-read generic signatures.
- **`TimeProvider`** for the hot-reload debounce (`HotReloadManager.cs:17,47,78`) — a 1:1 API swap
  that would make the 0.5 s `MinWaitTime` testable without `Thread.Sleep`. Take it only if you
  intend to test the debounce; it needs `Microsoft.Extensions.TimeProvider.Testing` added to
  `Directory.Packages.props` and the repo uses `TimeProvider` nowhere.
- **`MemoryPool<byte>` + `using`** instead of the `ArrayPool` `try/finally` — `SequenceReaders.Create`
  takes a `byte[]`, so you would need `MemoryMarshal.TryGetArray` to get back out. Worse than what
  it replaces.
- **`IAsyncEnumerable`, `field`, `required`/`init`, generic math.** No batch-load API to convert,
  no hand-written backing fields, no object-initializer surface, no numeric-generic code.

---

## Tests

Commit `5a4a056` deleted `AssetDecoderTests.cs`, `AssetEncoderTests.cs`, `AssetManagerTests.cs`,
`DummyTranscoder.cs` and `RepeatTranscoder.cs`, and nothing replaced them — there is no
`AssetPipeline` folder under `CapriKit.Tests` at all, though `CapriKit.Tests.csproj` still
project-references `CapriKit.AssetPipeline` and `Directory.Build.props:103` already grants
`InternalsVisibleTo`.

Per `source/CapriKit.Tests/README.md` the bar is the happy path on anything with some complexity.
A round-trip test — encode → decode → assert equal, plus a second `Load` that hits the cache —
against `InMemoryFileSystem` would be roughly 40 lines and would have caught B3 immediately.

`CapriKit.AssetPipeline` is also one of only two `source/` projects without a `README.md`, and it
is the one whose file format (`AssetEncoder.cs:9`) and hot-reload threading model most need a page
of prose.

---

## Suggested order

1. **B1 and B2** — nothing runs without them.
2. **Write back the happy-path round-trip test.** It is the thing that turns the rest of this list
   from review comments into a safety net.
3. **B3, B4** — small, mechanical, and each one is a resource leak.
4. **Decide the threading model (C1)** before touching `AssetCache` further; it determines whether
   the `Lock` stays at all, and P3 depends on the answer.
5. **D3, D4, M1** while there are still zero call sites and the breaking changes are free.
6. **P1 and P4** — the two largest performance wins, and P1 subsumes several smaller items.
7. The documentation pass (D1, D2, D6) and the error-message cleanup (D7).
