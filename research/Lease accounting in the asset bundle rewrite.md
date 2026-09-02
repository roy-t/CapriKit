# Lease accounting in the asset bundle rewrite

_Written 2026-08-28, on the `feature/asset_pipeline` branch. The `Playground/AssetBundle.cs` sketch had just
been promoted into the real API: `AssetManager.CreateBundle()` + `bundle.Load<T>()` +
`AssetBundleLoader<T>` became `AssetBundleBuilder<TContents>` + `builder.Request<T>()` + `AssetBundle<T>`.
Handles lost their state, `TrackedAsset` moved into `HotReloadManager`, and the manager started delivering
results by pushing them into an `IAssetRequester` instead of resolving handles it owned. This note reviews
what that rewrite broke._

## The shape of the rewrite

The good parts first, because they are what made the bug easy to miss.

An `AssetHandle<T>` is now a value — an asset id plus the identity of the builder that made it — with no
state and no lifetime. A bundle knows it is ready because it received as many results as it requested, not
by polling its handles. And because the builder issues the loads when `Build` runs, every request lands on
a bundle that already exists, so a cached asset can be delivered during `Build` itself and the set a bundle
waits for is complete before the first result can arrive.

`AssetBundleBuilder.Request` also refuses the same asset twice, so **a bundle holds exactly one lease per
asset**. That single rule deleted a whole category of lease-counting from the old code. It is also what made
the comment below look obsolete.

## Root cause: the pool evicts at zero

`AssetPool.Return` does not just decrement. At zero it *removes the entry* and queues the instance for
disposal:

```csharp
entry.RefCount--;
if (entry.RefCount <= 0)
{
    Entries.Remove(id);
    PendingDispose.Enqueue(entry);
}
```

So the refcount for one asset must never touch zero while there is still someone to hand it to. The old
`Update` knew this and said so:

> _Counted first and returned after, because resolving takes one lease per handle and the pool evicts at
> zero: returning as we went would drop an asset that two handles of the same bundle asked for to zero in
> between, and queue it for disposal twice._

The rewrite dropped that comment along with the counting, because `Request` refusing duplicates made the
*within-one-bundle* case impossible. But **two different bundles joining the same in-flight request** is the
identical shape, and `Outstanding` exists precisely to let them:

```csharp
// AssetManager.Update
foreach (var requester in waiting)
{
    var asset = result.Materialize!();   // PutOrLease  -> +1
    Deliver(result.Id, requester, success);
    // Deliver's refusal path calls Cache.Return -> -1, possibly to zero, inside the loop
}
```

`Materialize` is `TrackAndTakeLease`, which calls `Cache.PutOrLease`. When the previous iteration already
evicted the entry, `PutOrLease` finds nothing and **re-adds the very same instance** at refcount 1 — while
that instance is already sitting in `PendingDispose`.

## The three defects

All three were reproduced against the code as it stood on 2026-08-28. Repros are now in
`AssetManagerTests.cs` (see *Status* below).

### 1. A failed load returns a lease it never took

`Deliver`'s refusal path called `Cache.Return(id)` unconditionally. A failed load never put anything in the
pool, so the pool threw:

```
InvalidOperationException: Returned Hello.txt which was not found in the cache.
   at AssetPool.Return(AssetId id) in AssetPool.cs:line 107
   at AssetManager.Deliver(...) in AssetManager.cs:line 132
   at AssetManager.Update() in AssetManager.cs:line 204
```

Trigger: a bundle requests an asset, the build fails, the bundle is disposed before the next `Update`.
`AssetBundle.Dispose` already got this right (`if (result.IsSuccess) { leased.Add(id); }`); `Deliver` did
not.

### 2. Double dispose when two waiting bundles both refuse

Two bundles join one in-flight request, both are disposed before it arrives:

1. materialize → refcount 1
2. refused → `Return` → 0 → entry removed, instance queued for dispose
3. materialize → `PutOrLease` finds no entry → re-adds the same instance at 1
4. refused → `Return` → 0 → queued for dispose **again**

`DisposeReleased` then disposes it twice. Observed: `decoded=1 disposeCounts=[2]`. For a real GPU asset that
is a double free.

### 3. Use-after-dispose when only the first of two refuses

Same first three steps, but the second bundle *accepts*. It now holds a lease on an instance that is already
in `PendingDispose`, and `Cache.DisposeReleased()` at the bottom of that same `Update` disposes it.

Observed: `live bundle asset disposed while still leased? True`.

Note this is **order dependent**. If the live bundle comes first the sequence is 1 → 2 (accepted, no return)
→ 2 → 1 and everything is correct. Only the disposed-first ordering breaks, which is the kind of asymmetry
that survives a test suite.

## Status

All three are fixed, and all three repros are in `AssetManagerTests.cs`:

| test | defect |
|---|---|
| `Update_ReturnsNoLeaseForAFailedLoadThatItsBundleRefuses` | 1 |
| `Update_ReturnsOneLeasePerBundleWhenEveryWaitingBundleRefuses` | 2 |
| `Update_KeepsAnAssetAliveWhenOnlyTheFirstWaitingBundleRefuses` | 3 |

The first attempt was a guard on `Deliver`'s refusal path:

```csharp
else if (result.IsSuccess)
{
    Cache.Return(id);
}
```

That fixes defect 1 and nothing else, which is worth remembering: defects 2 and 3 are **pure success paths**, so
`result.IsSuccess` is true on every iteration and the guard is transparent to them. They are not about
*whether* the lease is returned but about *when*. They need the return to move
out of the per-requester loop, the way the old code did it:

```csharp
var refused = 0;
foreach (var requester in waiting)
{
    if (result.Failure is not null) { Deliver(result.Id, requester, failure); }   // took no lease
    else if (!Deliver(result.Id, requester, Success(result.Materialize!()))) { refused++; }
}

// After the loop: the pool evicts at zero, so returning as we go would drop the asset to zero
// between two waiters and queue the same instance for disposal twice.
for (var i = 0; i < refused; i++) { Cache.Return(result.Id); }
```

`Deliver` returns whether the requester accepted and stops returning leases itself. `Load`'s cache-hit path
then needs `if (!Deliver(...)) { Cache.Return(id); }`, which is safe there — one requester, no loop to cross
zero in. This is what shipped.

## Secondary findings

**`Update` throwing on a missing `Outstanding` entry** (since reverted to `continue`). It was reachable:
`RequestAsset` writes the success into `Incoming` and *then* logs, so a throwing log sink makes
`FireAndForget` write a second `Failed` result for the same id. Narrow, but the cost was out of proportion —
the throw sits inside the `while`, so the rest of `Incoming` stays queued, the remaining waiters in
`waiting` never get their result (and never will, `Outstanding` was already removed), and
`Cache.DisposeReleased()` + `HotReloadManager.Update()` are skipped for that frame. The same blast radius
applied to defect 1's exception.

**`RequestAsset`'s threading doc** briefly claimed it must be called while holding `RequestLock`. It runs on
the thread pool via `Task.Run` and holds no lock — it cannot, it awaits. Since corrected.

**`OneOrMany`'s deleted gotcha.** The removed sentence — _"Any insert into that dictionary can resize it and
invalidates such a reference, so use it immediately and never store it"_ — was the non-obvious half of the
remark. Without it the `GetValueRefOrAddDefault` sample reads like a general recipe. `AssetManager.Load`
happens to use the ref immediately; that is the constraint the deletion stopped recording.

**Smaller things.** `AssetBundle.Loaded` returns 0 after `Dispose` (it clears `Received`) while `Total` keeps
its value, and neither throws, unlike `IsReady`. `ThrowOnFailedAssets` leaving `isReady` false is
load-bearing — `Load_RetriesAnAssetWhoseFirstLoadFailed` depends on a failed bundle re-reporting its
failure — but the comment saying so is gone. `HotReloadManager.cs` ended up with mixed line endings (310 LF
+ 77 CRLF) against `.editorconfig`'s `end_of_line = crlf`, which is why an 80-line pure move renders as a
642-line rewrite in the diff and in blame.

## One owner for the lease list

`AssetManager` and `AssetBundle` both track what a bundle successfully leased, in two different shapes:

| | holds | used for |
|---|---|---|
| `Registration.Leases` | an `int` | the never-unloaded report at shutdown |
| `AssetBundle.Received` | `Dictionary<AssetId, JobResult<object>>` | `IsReady`, `Get`, `ThrowOnFailedAssets`, **and** building the `leased` list for `Unload` |

`Received` mostly is not bookkeeping — it is where the bundle keeps its payload, and the factory reads it
through `AssetResolver`. Only the last use overlaps with the manager. But the two halves are derived
independently (`Deliver` counts, `Dispose` lists) and nothing checks that they agree.

Moving the list to the manager collapses that. This has since been done:

```csharp
private sealed class Registration(string origin)
{
    public string Origin { get; } = origin;
    public List<AssetId> Leased { get; } = [];   // was: int Leases
}
```

- `Deliver` appends on the same condition it already uses to increment the counter.
- `Unload(IAssetRequester requester)` loses its `IReadOnlyList<AssetId>` parameter and returns
  `registration.Leased` itself.
- `AssetBundle.Dispose` stops walking `Received` for successes; `Received.Clear()` becomes purely about
  releasing references.
- `ReportAssetsThatWereNeverUnloaded` reads `Leased.Count`.
- The silent `if (!LiveRequesters.Remove(requester)) { return; }` drop-on-the-floor path disappears, because
  there is no caller-supplied list left to drop.

The manager is now the single authority on who leases what, and a bundle can no longer hand back a list that
disagrees with what it was given. Cost is one `List<AssetId>` per live requester instead of an `int`, which
is nothing at ~40 assets per bundle.

`Deliver` also gained a `Debug.Assert` on the registration being present. The lookup used to feed a log
line, so tolerating a miss was harmless; it now decides whether the asset is ever freed, so a miss is a leak
and worth catching in development.
