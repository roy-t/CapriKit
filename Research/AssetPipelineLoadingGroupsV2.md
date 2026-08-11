# Asset Pipeline — Loading Groups V2 (bundles and promises)

Status: design correction, 2026-08-11. Supersedes the Option C sketch in
`AssetPipelineLoadingGroups.md`; that document's framing (engine vs content assets, why shaders
can't use placeholders, the transcoder threading rules) still stands.

## Context

Continuing `source/CapriKit.AssetPipeline/vNext/`. The V1 sketch used `Task`/`await` for group
completion. That was dropped in favour of two explicit consumption styles:

- **block** — assets the caller cannot function without (bootstrap, engine systems)
- **poll** — assets the caller can proceed without, so the main thread keeps running a loading screen

`AssetGroup` became `AssetBundle<T>`, `Ticket<T>` became `Promise<T>`, and `AssetGroupBuilder` was
dropped in favour of `AssetManager.Load` returning promises directly.

That last change is what caused the deadlock in the design:

```csharp
// AssetBundle.cs — the shape that doesn't work
var a = assetManager.Load<string, NoSettings>(id, default);
var b = assetManager.Load<string, NoSettings>(id, default);
return assetManager.Bundle(r => new ExampleAssets(r.Get(a), r.Get(b)));
```

The bundle needs the promises to exist first (they're captured in the resolver closure), but
`AssetBundle.OnRequestCompleted` means each promise needs a back-pointer to the bundle. And if a
load finishes before `Bundle(...)` is called, `Update()` has nothing to signal — the countdown
never reaches zero.

## Diagnosis

Three problems were tangled into one. Only the first is about ordering, and it is self-inflicted.

### 1. The cycle exists only because the bundle wants to be *told*

`OnRequestCompleted` is a push notification. Push requires the observer to exist before the event.
Delete the notification and the ordering constraint goes with it — the bundle reads the state of its
promises instead of counting events:

```csharp
public bool Check([MaybeNullWhen(false)] out T value)
{
    if (this.result is null)
    {
        foreach (var promise in this.Promises)
        {
            if (!promise.IsResolved) { value = default; return false; }
        }
        this.result = Factory(new PromiseResolver(this));   // exactly once
    }
    value = this.result;
    return true;
}
```

What this removes:

- **The chicken-and-egg.** Promises never reference the bundle while loading. Build order is free.
- **"Already loaded before the bundle existed."** A promise resolved at birth from the cache is
  simply resolved; the scan sees `true`. There is no missed edge to compensate for, because the
  bundle reads a *state*, not a stream of events.
- **`CountdownEvent`, `OnRequestCompleted`, and the `Bundle` half of the `OutstandingPromises`
  tuple** (`AssetManager.cs:8`).

Cost is O(n) per `Check` with n = 2–20 promises per bundle, against O(1) for the counter. Worth it.
If bundle counts ever grow into the thousands, reintroduce the counter — but it must then be
initialised at *build* time from the promises that are not yet resolved, which is the same
reconciliation done eagerly.

### 2. The in-flight table is 1:1, but the relationship is 1:N

`Dictionary<AssetId, (AssetBundle, Promise)>` holds one promise per id. Two bundles can want the
same asset, and one bundle can want it twice — `ExampleAssets.Define` requests
`new AssetId("key", "path")` twice today, so the second `Load` would overwrite the first entry and
promise `a` would hang forever. This was the `// TODO: what if 2 promises wait for the same thing`.

Make the in-flight table the dedupe point:

```csharp
private readonly Dictionary<AssetId, List<Promise>> InFlight = [];

internal Promise<TAsset> Request<TAsset, TSettings>(AssetId id, TSettings settings)
    where TAsset : class
{
    var promise = new Promise<TAsset> { Id = id };

    if (Cache.TryLease<TAsset>(id, out var cached))
    {
        promise.Value = cached;          // resolved before it was ever outstanding
        return promise;
    }

    if (this.InFlight.TryGetValue(id, out var waiting)) { waiting.Add(promise); }
    else { this.InFlight[id] = [promise]; Dispatch<TAsset, TSettings>(id, settings); }

    return promise;
}

public void Update()
{
    while (Ready.TryRead(out var kv))
    {
        if (!this.InFlight.Remove(kv.Id, out var waiting)) { continue; }

        var asset = Cache.PutOrLease(kv.Id, kv.Asset);
        foreach (var promise in waiting) { promise.Value = asset; }
        // snip: take waiting.Count - 1 extra leases
    }
    Cache.Collect();
}
```

Two requests for one id now cost one disk read and fill both promises.

**Refcount detail:** `AssetCache.PutOrLease` takes exactly one lease. N promises that will each
`Return` once on bundle disposal need N−1 additional `TryLease` calls here, or `AssetCache.Dispose`
throws on the asymmetry.

### 3. The builder from V1 was dropped and is still needed

`AssetGroupBuilder` in V1 gave the group an identity before its tickets. Moving `Load` onto the
manager is what made the ordering feel impossible. Pull (fix 1) already resolves the ordering, but
the builder is still wanted for three other reasons: it defines which promises a bundle **owns**
(for `Dispose` → `Cache.Return`), it supplies the resolver-ownership check, and it makes "when does
loading start" explicit.

```csharp
public sealed class AssetBundleBuilder(AssetManager manager)
{
    private readonly List<Promise> Promises = [];

    public Promise<TAsset> Load<TAsset, TSettings>(AssetId id, TSettings settings)
        where TAsset : class
    {
        var promise = manager.Request<TAsset, TSettings>(id, settings);
        this.Promises.Add(promise);
        return promise;
    }

    public AssetBundle<T> Build<T>(Func<PromiseResolver, T> factory) where T : notnull
    {
        var bundle = new AssetBundle<T>(manager, [.. this.Promises], factory);
        foreach (var promise in this.Promises) { promise.Owner = bundle; }
        return bundle;
    }
}
```

```csharp
public static AssetBundle<ExampleAssets> Define(AssetManager assetManager)
{
    var builder = assetManager.CreateBundle();
    var a = builder.Load<string, NoSettings>(new AssetId("a", "example.txt"), default);
    var b = builder.Load<string, NoSettings>(new AssetId("b", "example.txt"), default);

    return builder.Build(r => new ExampleAssets(r.Get(a), r.Get(b)));
}
```

Loading starts at `Load`, not at `Build` — there is no reason to defer, because an early completion
is now harmless. `Owner` is assigned in `Build`, which closes V1's open question on promise
provenance: `PromiseResolver` takes the bundle (`new PromiseResolver(this)`) and `Get` asserts
`promise.Owner == owner` rather than the commented-out check in `Promise.cs:15`.

## The deadlock that hasn't been hit yet

Independent of the above, and the most dangerous item here because it only appears once `Update` is
wired into the game loop.

`AssetBundle<T>.Wait` blocks on `Outstanding.Wait()`. `OnRequestCompleted` fires from
`AssetManager.Update()`. In the bootstrap case both are the main thread: it blocks waiting for a
signal only it could deliver.

This forces a decision about **who applies completions**. Keep it on the main thread — that gives
lock-free `Promise.Value` and `InFlight`, and it is where a transcoder's `ID3D11DeviceContext`
finalisation step has to run anyway (see the transcoder constraints in V1). Blocking then means
*pumping*, not sleeping:

```csharp
public T Wait(CancellationToken cancellationToken = default)
{
    var spin = new SpinWait();
    while (!Check(out var value))
    {
        cancellationToken.ThrowIfCancellationRequested();
        Manager.Update();     // the waiting thread does the work instead of sleeping on it
        spin.SpinOnce();
    }
    return value;
}
```

This is the helper pattern from job systems (Unity's `JobHandle.Complete`), and it collapses both
consumption styles onto one mechanism: `Wait` is `Check` in a pumping loop, `Check` is one test
against a drain the game loop already performed. No `await`, no `CountdownEvent`, no kernel wait.

**Constraint to document:** `Wait` and `Check` are main-thread only. If a worker should later build
a level off-thread (V1 option D), give that path a real `ManualResetEventSlim` signalled from
`Update`.

## Two bugs in the current code, independent of the redesign

**`Check` re-materialises the bundle.** `AssetBundle.cs:47` calls `Wait()`, which calls
`Resolver(...)`, so every successful `Check` constructs a *new* `ExampleAssets`. Polling it for 60
frames on a loading screen yields 60 distinct instances, and hot reload has no stable object to swap
into. Materialise once and cache (`this.result` above) — this is what V1's `Materialize()` was for.

**No faulted state.** `Promise` has only `Value`. Give it an `ExceptionDispatchInfo?` alongside, and
treat faulted as resolved. Otherwise one missing file makes `Wait` spin forever instead of throwing,
and the promises that did land leak their leases into `AssetCache.Dispose`. V1 already called this
out ("let the counter reach zero anyway"); with pull it becomes "let the promise resolve as
faulted".

## Nullability footnote on `Check`

The original signature `bool Check([NotNullWhen(true)] out T? value)` fails to compile under this
repo's `<WarningsAsErrors>nullable</WarningsAsErrors>` (`Directory.Build.props:12`) with **CS8762**,
*"Parameter 'value' must have a non-null value when exiting with 'true'"* — verified against the
SDK.

For an unconstrained type parameter, `T` and `T?` have the same null-state: `T` could be
instantiated as `string?`, so the compiler cannot prove `Wait()` returns non-null. `[NotNullWhen]`
is a promise with nothing to back it. Either use the canonical `TryGetValue` shape
`[MaybeNullWhen(false)] out T value` (no constraint needed), or keep `where T : notnull` on
`AssetBundle<T>`, which the current code already has and which makes the original signature legal.
`notnull` is worth keeping regardless — a resolved bundle that is null is meaningless.

## Resulting data model

```csharp
public abstract class Promise
{
    internal AssetId Id { get; init; }
    internal object? Value;                  // written on the main thread only
    internal ExceptionDispatchInfo? Error;   // faulted counts as resolved
    internal AssetBundle? Owner;             // assigned in Build, for the provenance check
    internal bool IsResolved => Value is not null || Error is not null;
}

public abstract class AssetBundle;   // no counter, no OnRequestCompleted

public sealed class AssetBundle<T> : AssetBundle where T : notnull
{
    private readonly AssetManager Manager;
    private readonly Promise[] Promises;     // also the Dispose → Cache.Return set
    private readonly Func<PromiseResolver, T> Factory;
    private T? result;                       // materialised once
    // snip: Check, Wait, Dispose
}

public sealed partial class AssetManager
{
    private readonly Dictionary<AssetId, List<Promise>> InFlight = [];
    private readonly LightweightChannel<(AssetId Id, object Asset)> Ready = new();
    private readonly AssetCache Cache = new();
    // snip: CreateBundle, Request, Update, Dispose
}
```

## Open questions

- Cancellation: dropping a bundle mid-load leaves entries in `InFlight` whose promises nobody reads.
  Harmless (the drain skips them) but it means the load still completes and takes a lease. Does
  `AssetBundle.Dispose` need to prune `InFlight`, or is letting it land and immediately `Return` it
  simpler?
- Hot reload re-entry is unchanged from V1 and still unanswered: a reloaded asset inside a live
  bundle needs `HotSwap`, not re-materialisation, because `this.result` has already been handed out.
- Does `TryLoad` (`AssetManager.cs:12`) survive at all? With `Request` checking the cache inline it
  looks redundant — V1 already suspected it should be internal.
- `SpinWait` in the `Wait` pump busy-burns a core during bootstrap. Probably fine for a few hundred
  ms at startup; revisit if boot loading grows.
