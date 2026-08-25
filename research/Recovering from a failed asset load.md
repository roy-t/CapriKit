# Recovering from a failed asset load

_Written 2026-08-25, on the `feature/asset_pipeline` branch. We had just added
`AssetManagerTests.Load_RetriesAnAssetWhoseFirstLoadFailed`, which fails on purpose, to keep this bug from
being forgotten. This note is the plan for fixing it properly._

## What is actually broken

Three separate defects, all in the same handful of lines. Only the first one is the wedge, but a fix that
does not address the other two just moves the problem.

**1. The request is never cleared.** `AssetManager.Update` only removes the `Outstanding` entry on the
success path:

```csharp
while (Incoming.TryRead(out var result))
{
    var (id, trackAndTakeLease) = result;
    var handles = Outstanding[id];
    // snip: resolve every handle
    Outstanding.Remove(id);   // never reached when the request failed
}
```

A failure travels through a *different* route: `LightweightChannel.Write(ExceptionDispatchInfo)` puts it in
a second queue, and `TryRead` drains that queue first and throws. So `Update` throws before it can clean
anything up, `Outstanding[id]` survives, and because `Load` hands every later request for that id to the
existing list instead of starting a new one, the asset can never be loaded again.

**2. The error does not know which asset it belongs to.** The exception queue in `LightweightChannel<T>` is
untyped and separate from the item queue, so by the time `Update` catches it there is no id to clean up
even if we wanted to. This is why the fix cannot be local to `Update` as it stands.

**3. A failure takes down more than the asset.** `Update` throws from inside the lock, which skips
`Cache.DisposeReleased()` and `HotReloadManager.Update()` for that frame, and abandons any *successful*
loads still sitting in the queue behind the failure.

## The half that is not a choice

Whatever policy we pick, the cleanup has to happen **on the main thread, inside the same `RequestLock`
section that `Load` uses**, and the failure has to carry its id to get there. Route both outcomes through
the one queue:

```csharp
// One item per finished request, successful or not, so Update sees both the same way.
internal readonly record struct LoadResult(AssetId Id, Func<object>? Materialize, ExceptionDispatchInfo? Error);
```

`Load`'s failure handler stops using the channel's exception queue:

```csharp
Task.Run(() => RequestAsset<TAsset, TSettings>(id, settings)).FireAndForget(
    ex =>
    {
        LogFailed(Logger, id);
        Incoming.Write(new LoadResult(id, null, ex));   // was: Incoming.Write(ex)
    });
```

and `Update` clears the entry for both outcomes:

```csharp
lock (RequestLock)
{
    while (Incoming.TryRead(out var result))
    {
        // Removing here is the whole fix: a handle either made it into this list before we took the lock,
        // or it misses the list entirely and its Load starts a fresh request.
        if (!Outstanding.Remove(result.Id, out var handles)) { continue; }

        if (result.Error is not null) { /* policy, see below */ continue; }

        foreach (var handle in handles) { handle.Resolve(result.Materialize!()); }
    }
}
```

After this, `LightweightChannel.Write(ExceptionDispatchInfo)` is unused by the asset pipeline. Leave the
overload alone (it is a general-purpose primitive), but know that this path no longer relies on it.

## The half that is a choice: what the app sees

All three options below build on that same core. **A and B both give you the "proper exception that quits
the app if uncaught" behaviour** you asked for; they differ in *where* it is thrown.

### Option A: `Update` throws

```csharp
if (result.Error is not null)
{
    // The entry is already gone, so a later Load starts a clean request even if somebody catches this.
    throw new AssetLoadException(result.Id, result.Error.SourceException);
}
```

Smallest possible diff on top of the core: no changes to `AssetHandle` or `AssetBundleLoader`. The handles
of the failed request are simply left unresolved forever.

That last part is the catch. It is fine while the exception is uncaught and the process dies, but an app
that catches it is left with a bundle that never completes and no way to find out why — a silent hang.
Option A is therefore *only* correct as a fail-fast policy; catching it is unsupported. It also keeps
defect 3: the throw still skips `DisposeReleased` and the hot-reload update for that frame.

### Option B: the handle carries the failure, `IsReady` throws (recommended)

Give `AssetHandle` an error next to its value. Writing `error` before the `volatile` `isResolved` gives the
same release ordering the existing `value` write relies on:

```csharp
private Exception? error;
internal Exception? Error => error;

internal void Fail(Exception ex)
{
    Debug.Assert(isResolved == false);
    error = ex;
    isResolved = true;   // volatile write publishes error too
}
```

`Update` fails the handles instead of throwing, so it stays a pure bookkeeping call:

```csharp
if (result.Error is not null)
{
    foreach (var handle in handles) { handle.Fail(result.Error.SourceException); }
    continue;
}
```

and `AssetBundleLoader<TBundle>.IsReady` treats a failed handle as arrived, then throws once everything is
in:

```csharp
for (var i = Pending.Count - 1; i >= 0; i--)
{
    var handle = Pending[i];
    if (!handle.IsResolved) { continue; }
    if (handle.Error is not null) { (failures ??= []).Add(new AssetLoadException(handle.Id, handle.Error)); }
    // snip: existing swap-remove
}

if (Pending.Count > 0) { value = default; return false; }

// Everything arrived but some of it failed. Throwing here puts the error in front of the code that
// actually wanted the asset, rather than in the middle of the manager's bookkeeping.
if (failures is not null) { throw failures.Count == 1 ? failures[0] : new AggregateException(failures); }
```

`IsReady` is called from the game loop, so an uncaught `AssetLoadException` still takes the process down —
your requirement is met. What you gain over A is that `Update` never throws, so the cache collection and
the hot-reload pass always run, and the failure is attributed to a specific bundle and asset.

Note that `IsReady` will throw again on every subsequent call, since `Pending` is empty but `isReady` was
never set. That is the right idempotent behaviour, but it means a caught exception throws once per frame.

### Option C: the loader reports, nothing throws

Same `AssetHandle.Fail` as B, but the loader exposes the failure instead of throwing:

```csharp
public bool IsFaulted => Failures.Count > 0;
public IReadOnlyList<AssetLoadException> Failures => failures ?? [];
```

`IsReady` returns false forever while faulted. This is what you want the day an app wants to substitute a
placeholder asset and carry on. It is the wrong *default*: an app that forgets to check `IsFaulted` sits on
a loading screen forever with nothing but a log line to show for it.

Worth building **on top of** B rather than instead of it — B can grow `IsFaulted` and a non-throwing
`TryGetResult` later without changing what the throwing path does.

## The load / hot-reload asymmetry

Good news: the hot-reload half already behaves the way you want, and no option above changes it.
`HotReloadManager.FinishReload` catches everything, logs it, keeps the live asset's contents and returns
the lease in a `finally`; the faulted task's `.Exception` is read by `LogReloadFailed`, which marks it
observed so it never reaches `TaskScheduler.UnobservedTaskException`. Hot reload does not use `Incoming` at
all, so failures there cannot leak into the load path.

What the fix should add is the *statement* of that split, because the same transcoder bug now produces a
hard crash in one path and a log line in the other, and that looks arbitrary until you say why:

- **Load**: there is no valid asset to fall back on, so the app cannot sensibly continue. Fail fast.
- **Hot reload**: there is a perfectly good asset already live, and this is a development-time convenience.
  Never take down the game over it.

Concretely: document that contract on `IAssetTranscoder<TAsset, TSettings>.Encode` and `.Decode`, and add a
regression test that a hot-reload failure does not throw out of `AssetManager.Update()` (today
`HotReloadManagerTests.Update_RebuildFails` only exercises `HotReloadManager` directly, never through the
manager). Under option A that test is load-bearing, because `Update` becomes a method that sometimes throws.

## Side cleanup worth folding in

`RequestAsset` calls `GetTranscoder<TAsset, TSettings>()` on the thread pool, so a missing or mismatched
transcoder — a programmer error, not an asset failure — arrives through the same failure path as a corrupt
file. Hoisting that call into `Load` makes it throw synchronously, on the calling thread, with a clean
stack, before any `Outstanding` entry exists. Small and independent of the options above.

## Open question: retrying a permanently broken asset

Once the wedge is gone, every `Load` of a broken asset starts a fresh build. In practice `Load` is called
once per bundle rather than once per frame, so this is unlikely to become a rebuild storm — but nothing
stops it either. If it ever bites, the cheap answer is to remember failed ids with a timestamp and refuse to
retry within a few seconds, or to require an explicit `assetManager.Forget(id)`. Not worth building yet.

## Which one

| | A: `Update` throws | B: `IsReady` throws | C: loader reports |
|---|---|---|---|
| Wedge fixed | yes | yes | yes |
| Kills the app if uncaught | yes | yes | no |
| Error names the bundle and asset | id only | yes | yes |
| `Update` stays non-throwing | no | yes | yes |
| Cache collect + hot reload still run that frame | no | yes | yes |
| App can recover deliberately | no (silent hang) | no | yes |
| Touches | `Update` | `Update`, `AssetHandle`, `AssetBundleLoader` | B, plus loader API |

Go with **B**. It gives you the fail-fast crash you asked for, but throws it where the app asked for the
asset instead of in the middle of the manager's bookkeeping, which keeps defect 3 fixed as well. A is worth
knowing about as the two-line version if you want the wedge gone today and the rest later. C is the natural
follow-up once something actually wants to survive a missing asset.

## Test impact

`Load_RetriesAnAssetWhoseFirstLoadFailed` goes green on `Attempts == 3` under all three options. Its last
assertion is the one that changes:

- **A**: unchanged — the failed bundle's handles stay unresolved, so `IsReady` is still `false`.
- **B**: becomes `await Assert.That(() => failedLoader.IsReady(out _)).Throws<AssetLoadException>();`
- **C**: `IsReady` stays `false` and `failedLoader.IsFaulted` is `true`.

Two tests worth adding while in here:

1. A hot-reload failure does not throw out of `AssetManager.Update()` (the asymmetry above).
2. Two concurrent `Load` calls for the same failing id both get failed, and neither is silently dropped —
   this is the race that the naive fix gets wrong, see below.

## Considered and rejected: clearing `Outstanding` from the thread pool

The obvious two-line fix, and the one I probed with while checking that the new test can go green:

```csharp
ex =>
{
    LogFailed(Logger, id);
    lock (RequestLock) { Outstanding.Remove(id); }
    Incoming.Write(ex);
});
```

It does turn the suite green, which is exactly why it is worth writing down as a trap. It races:

1. Thread A calls `Load(id)`, creates `Outstanding[id] = [h1]` and starts the request.
2. The request fails; the continuation queues up on `RequestLock`.
3. Thread B calls `Load(id)`, wins the lock, sees the entry is still there and adds `h2` to it. B believes
   its load is in flight.
4. The continuation takes the lock and removes the entry — dropping **both** `h1` and `h2`.

`h2` now never resolves and no request was ever started for it, so the wedge has been traded for a silently
dropped handle, which is harder to notice. Doing the removal on the main thread inside `Update`, in the
same lock section that materializes successes, is what closes this: `h2` is either in the list we are about
to fail, or it missed the list and its own `Load` started a fresh request. There is no third case.
