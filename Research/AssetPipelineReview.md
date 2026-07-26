# Asset Pipeline Review

> Multi-agent review of `source/CapriKit.AssetPipeline` (branch `feature/asset_pipeline`),
> triggered by a worry that the caching, hot-reloading and `AssetJob<T>` handling were
> too complex and mixed too many paradigms. Three review agents (correctness,
> clarity/paradigm-consistency, simplification) assessed the source without running any
> tools; every finding below was cross-checked against a manual read of the code.

**Headline:** the worry was right for a concrete reason. The non-exhaustive `AssetJob`
consumption API (`OnSuccess`/`OnFailure`/`OnMissing`) is not just noisy — it *directly
caused* three correctness bugs by letting success paths fall through to `throw`. The
paradigm problem and the correctness problem are the same problem.

## Overview

| # | Cluster | Finding | Severity | Location |
|---|---------|---------|----------|----------|
| F1 | A · Result API | Successful full build never returns — falls through to `throw` | Critical | `AssetManager.cs:94-105` |
| F2 | A · Result API | Rebuild failure check inspects the **wrong** job variable | High | `AssetManager.cs:100` |
| F3 | A · Result API | Successful hot-swap falls through to `throw` (logged as failure) | High | `HotSwappable.cs:18-32` |
| F4 | C · Lifetime | `PopScope` mutates the dictionary while enumerating it | High | `AssetMemoryCache.cs:67-74` |
| F5 | B · Errors | `OpenRead` + `checked` cast throw *outside* the catch, escaping the `Failure` contract | Low | `AssetDecoder.cs:24-25` |
| F6 | A · Result API | `AssetJob` offers 4 consumption modes; the `On*` trio is non-exhaustive | High | `Asset.cs:48-94` |
| F7 | B · Errors | Five error paradigms; an exception is captured -> ferried -> re-thrown | High | decoder -> manager |
| F8 | B · Errors | The `Failure`/EDI state is probably unnecessary | Medium | `Asset.cs`, `AssetManager.cs` |
| F9 | A · Simplify | `AssetFileCache.Load`'s `Match` is an identity on 2 of 3 arms | Medium | `AssetFileCache.cs:14-25` |
| F10 | A · Simplify | `SettingsEqual` re-serializes both sides on every load | Medium | `AssetFileCache.cs:48-57` |
| F11 | A · Simplify | The "success->register / else rethrow / else throw" tail is duplicated | High | `AssetManager.cs` + `HotSwappable.cs` |
| F12 | C · Lifetime | Two ownership models (scope-stack vs `WeakReference`); pop doesn't untrack | High | `AssetMemoryCache` + `HotSwapManager` |
| F13 | C · Threading | `Track` mutates plain dictionaries off the main thread | Medium | `HotSwapManager.cs:49-53` |
| F14 | D · Naming | "Job", `On*`, and dual "Cache"/"Load" each name two things | Medium | cross-cutting |
| F15 | D · API surface | Public API leaks `Encode`/`Decode`/`AssetJob` internals | Medium | `AssetManager`, `Asset.cs` |
| F16 | D · API surface | `IAssetTranscoder` mixes public/internal members across two arities | Low | `IAssetTranscoder.cs` |

The four clusters map onto the three original worries: **A + B = the `AssetJob<T>`
handling**, **C = caching + hot reloading**, **D = the cross-cutting naming/surface tax**.
Fix A first — it contains the only shippable blockers.

---

## Cluster A — The result API, and the bugs it caused

`AssetJob<T>` lets you consume a three-state value with a **non-exhaustive** idiom
(`if (job.OnSuccess(out …)) { … }`), which silently collapses the other two states into a
fall-through. Nothing forces you to handle all three, so a missing `return` compiles
cleanly and ships.

### F1 — Successful build never returns *(Critical)*
`AssetManager.cs:94`

```csharp
var getFromFullBuild = await Decode<TAsset>(id);
if (getFromFullBuild.OnSuccess(out var freshAsset))
{
    Cache.Add(id, freshAsset.Value);
    HotSwapManager.Track(freshAsset, settings);
    // no return — control falls through …
}
// …
throw new Exception($"Asset {id} could not be found");   // reached on success
```

**Failure scenario:** memory miss + disk miss + a *successful* build -> the asset is
built, cached and tracked, then `Load` throws "could not be found". (A second call would
hit the memory cache and succeed, making the bug maddening to diagnose.) Every first-time
load of a not-yet-built asset throws.

**Fix:** add `return freshAsset.Value;` inside the success block.

### F2 — Wrong job variable in the failure check *(High)*
`AssetManager.cs:100`

```csharp
if (getFromFileCache.OnFailure(out var rebuildFailure)) { rebuildFailure.Throw(); }
```

After the rebuild, this inspects the **original file-cache** job, not `getFromFullBuild`.
So a genuine *build* error is discarded (you get the generic "could not be found"), while a
stale *cache* error can be re-thrown even though the rebuild is what actually ran.
**Fix:** check `getFromFullBuild.OnFailure(...)`. This also proves F8 — the "remember the
cache error and rethrow it later" behaviour isn't actually relied upon.

### F3 — Successful hot-swap throws *(High)*
`HotSwappable.cs:18`

Same shape: the `OnSuccess` block does the swap and re-tracks, but doesn't `return`, so it
falls to `throw new Exception($"Asset {Id} … could no longer be found")` on line 32. That
exception is caught by `HotSwapManager.HotSwapCompleted` and logged via `LogReloadFailed` —
so **every successful hot reload is reported as a failure**, after its side effects already
ran. **Fix:** add `return;` after the success block.

### F6 — Four ways to consume one value; two are unsafe *(High)*
`Asset.cs`

`AssetJob` exposes `OnSuccess`/`OnFailure`/`OnMissing` (imperative, **non-exhaustive**)
*and* two `Match` overloads (functional, **exhaustive**). Offering both means every reader
must learn two APIs and every author must pick the safe one unaided — and F1-F3 are what
happens when they don't. Secondary smell: `OnSuccess(out Asset<TAsset>? asset)` returns a
**nullable** even on success, so a caller who trusts the out over the bool invents yet
another path.

**Recommendation — keep the exhaustive one.** The whole point of the tri-state is to *not*
lose the Failure/Missing distinction, and only `Match` (or a `switch` on an explicit state
enum) enforces that.

- Delete the `On*` trio. Consume via `Match`, or add
  `enum AssetJobState { Success, Failure, Missing }` + a non-nullable payload accessor and
  `switch` on it — a `switch` expression still gets exhaustiveness warnings; three
  independent `bool`s never can.
- Make the success payload non-nullable.

### F11 — Unify the duplicated tail *(High)*
The "on success add/track/use; else rethrow EDI; else throw not-found" shape is hand-rolled
in **both** `AssetManager.Load` and `HotSwappable.HotSwap` — which is exactly why the same
fall-through slipped into both. Extract it once so it can't recur:

```csharp
private TAsset Register<T>(Asset<T> asset, IAssetSettings<T> settings) where T : class
{
    Cache.Add(asset.Id, asset.Value);
    HotSwapManager.Track(asset, settings);
    return asset.Value;
}

[DoesNotReturn]
private static TAsset Unavailable<T>(AssetJob<T> job, AssetId id) where T : class
{
    if (job.OnFailure(out var edi)) { edi.Throw(); }
    throw new AssetNotFoundException(id);
}
```

The `Load` tail then becomes
`return getFromFullBuild.OnSuccess(out var fresh) ? Register(fresh, settings) : Unavailable(getFromFullBuild, id);`
— F1 and F2 both become structurally impossible.

### F9 — Dead `Match` ceremony *(Medium)*
`AssetFileCache.Load` uses a 3-arm `Match` where the failure and missing arms both just
`return job` — only the success arm has logic. Collapse to early returns:

```csharp
var job = await AssetDecoder.Decode(id, transcoder, FileSystem);
if (job.OnSuccess(out var asset))
    return IsUpToDate(asset) && SettingsEqual(transcoder, asset.Settings, settings)
        ? job : AssetJob<TAsset>.Missing(id);
return job;   // failure and missing pass straight through
```

### F10 — `SettingsEqual` serializes twice per load *(Medium)*
It writes both the embedded and requested settings into fresh `ArrayBufferWriter<byte>`s
just to compare bytes — two allocations + two serializations on every disk hit. If settings
are `record`/`record struct`, compare structurally instead:

```csharp
// default method on IAssetTranscoder<TAsset,TSettings>
bool SettingsEqual(TSettings a, TSettings b) => EqualityComparer<TSettings>.Default.Equals(a, b);
```

Cheaper fallback if you keep bytes: `AssetDecoder` already read the settings bytes off disk
— stash them on the `Asset` and serialize only the *requested* side once.

---

## Cluster B — Too many error paradigms

### F7 — Five vocabularies, and a full round-trip *(High)*
The module speaks (1) plain `throw`, (2) `ExceptionDispatchInfo` capture-and-rethrow,
(3) the tri-state `AssetJob`, (4) `bool`-try (`TryGet`, the `On*` trio), and (5) nullable
`out`. They don't layer — they convert into each other in a circle: `AssetDecoder` catches
**every** exception and demotes it to `Failure(EDI)`; that failure rides through
`AssetFileCache`, is held across an `await` while `AssetManager` re-encodes, and is finally
re-thrown at the bottom of `Load`. Tracing "what happens on a corrupt file?" means holding
the demoted error in your head across the whole method.

**Fix:** make `AssetJob` the *only* currency inside the pipeline and convert to an exception
exactly once, at the single public `Load` seam — by `Match`-ing the **final** job (not
re-reading a stale one). Keep `ExceptionDispatchInfo` **only** where it earns its keep:
preserving a stack trace across the `await`/thread-pool hop in hot reload. Everywhere the
exception never crosses a thread, store a plain `Exception`.

### F8 — The `Failure` state may be dead weight *(Medium)*
`Failure` exists solely to carry an EDI so `Load` can *defer* a rethrow — but F2 shows that
deferral is buggy and unrelied-upon, and `Missing` vs `Failure` are otherwise treated
identically (both fall through to rebuild). Consider collapsing to **two states +
exceptions**: `Decode` returns `Asset<T>?` (`null` = missing) and simply *throws* on a
corrupt file; `Load` wraps the disk read in `try/catch`, logs, and rebuilds. That deletes
the `Failure` state, the EDI field, `OnFailure`, and all the remember-then-rethrow
bookkeeping. This is the single biggest reduction in paradigm count — decide first whether
you ever genuinely need "surface the *cache* error only when the *rebuild* also fails" (the
current code suggests not).

### F5 — Decoder error-path gap *(Low)*
`AssetDecoder.cs:24-25`: `fileSystem.OpenRead(...)` and `checked((int)input.Length)` run
*before* the `try` (line 28), so an IO/sharing error or a >2 GB `OverflowException` escapes
as a raw throw instead of the `Failure` job the method otherwise promises. **Fix:** move
both inside the `try` (or accept the inconsistency once F8 makes throwing the norm).

---

## Cluster C — Lifetime & threading

### F4 — `PopScope` crashes and leaks *(High)*
`AssetMemoryCache.cs:67`

```csharp
foreach (var (key, value) in Cache) { if (value.Scope >= scope) { value.Disposable?.Dispose(); Cache.Remove(key); } }
```

`Dictionary` bumps its version on `Remove`, so the next `MoveNext` throws
`InvalidOperationException: Collection was modified` — on essentially every real scope pop
that held an asset. The loop then aborts, so the remaining assets in that scope are never
disposed (leak). **Fix:** snapshot the keys first:

```csharp
foreach (var key in Cache.Where(kv => kv.Value.Scope >= scope).Select(kv => kv.Key).ToList())
{
    Cache[key].Disposable?.Dispose();
    Cache.Remove(key);
}
```

### F12 — Two ownership models that can disagree *(High)*
`AssetMemoryCache` says the **cache owns** assets (holds `IDisposable?`, disposes on pop).
`Reloadable` says they're **not owned** (holds only a `WeakReference<TAsset>`, treats
"collected" as normal). Both can't be the authority on "is this alive?" Because the cache
strongly holds the disposable for the whole scope, the weak-ref can't die *until* pop — and
crucially **nothing untracks the `Reloadable` from `HotSwapManager` on `PopScope`**. So
after a pop, `Tracked`/`Dependents` still reference the now-disposed asset; a file change
can hot-swap a **disposed instance** (this is the `// TODO: cold can be alive but disposed`).

**Fix:** make **scope the single owner**. Have `Reloadable`/hot-reload hold the *AssetId*
(a cache key) instead of a `WeakReference<TAsset>`, and look the live instance up in the
cache at reload time — reloads then naturally stop when the scope is popped. Add an
`Untrack(id)` called from `PopScope`. Reserve `WeakReference` only if you deliberately
support caller-owned assets the cache does *not* dispose (and document that split).

### F13 — `Track` races the main thread *(Medium)*
`HotSwapManager.Track` writes plain `Dictionary`s (`Tracked`, `Dependents`). It's documented
main-thread, but `AssetManager.Load` calls it *after* `await`s (lines 78, 94); with no
game-loop `SynchronizationContext` those continuations run on thread-pool threads, while
`ProcessUpdates` reads/enumerates the same dictionaries each frame on the main thread ->
data race / mid-enumeration throw. **Fix:** marshal `Track` onto the main thread (queue it
like hot-swaps and apply in `ProcessUpdates`), or guard both dictionaries with a lock.
(Severity depends on the threading model — if `Load` is always awaited on the main thread
with a context installed, this drops to Low.)

---

## Cluster D — Naming & public surface

### F14 — Names that each mean two things *(Medium)*
- **`AssetJob`** is not a job — it's an already-computed result, but "Job" implies something
  you `Start`/`await`. Rename -> `AssetResult` / `AssetOutcome`.
- **`On*`** is C#'s convention for *event handlers* (`OnClick`), yet here they're predicates
  — and the same words are the `Match` callback parameters. Rename survivors to the
  `Try*`/`Is*` convention.
- **"Cache"** names both the volatile in-memory store and the on-disk store; **"Load"**
  names both the full orchestration (`AssetManager.Load`) and decode-one-file
  (`AssetFileCache.Load`). Rename by role: `AssetStore` / `AssetDiskCache`, and give the
  file method a narrower verb (`TryReadFromDisk`).

### F15 — Public surface leaks the plumbing *(Medium)*
A user needs: register transcoders, `Load<T>(id)`, scope for lifetime. But `Encode`,
`Decode` (returning `AssetJob`), and thereby the whole tri-state/EDI machinery are public
too. **Fix:** make `Encode`/`Decode`/`AssetJob` `internal` (with `InternalsVisibleTo` for
tests). Every paradigm hidden from users is a finding that stops being their problem. This
also lets F9's `AssetFileCache` fold into a private `AssetManager.TryReadFromDisk` — after
the trims above it's ~5 lines and holds nothing the manager can't reach.

### F16 — `IAssetTranscoder` density *(Low)*
The two-arity settings-erasure is a legitimate, clever technique — but it mixes
accessibilities *inside* one interface (public `HotSwap` beside internal `Encode`) and
bridges via default-interface methods, so "how is a transcoder invoked?" needs two
interfaces read at once. **Fix:** move the erased members to an internal
`IAssetTranscoderCore`, and add one XML-doc line on it: "you implement
`IAssetTranscoder<TAsset,TSettings>`; the erased base is bridged for you."

---

## Suggested order of attack

1. **F1, F2, F3, F4** — four small, mechanical fixes; correctness blockers (add two
   `return`s, fix one variable, snapshot before removing).
2. **F11 + F6** — unify the tail and move to exhaustive consumption, so F1-F3 can't return.
3. **F8 + F7** — decide whether `Failure`/EDI stays; collapsing to nullable+throw erases
   most of the "too many paradigms" feeling.
4. **F12, F13** — lifetime authority + `Track` threading (the hot-reload half of the worry).
5. **F14, F15, F9, F10, F16** — naming, surface, and the smaller trims.

The three agents were unanimous that the tri-state result API is the load-bearing issue: it
caused the correctness bugs *and* it's the main source of the "multiple paradigms" feeling —
fix that cluster and most of the rest gets smaller on its own.
