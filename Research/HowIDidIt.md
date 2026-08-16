# How I did it: HotReloadManagerV3

_Context: we were finishing the asset pipeline on the `feature/asset_pipeline` branch. Loading assets already
worked end-to-end, hot-reloading did not. Three earlier attempts (`HotReloadManager`, `HotReloadManagerV2`,
`HotReloadPipeline`) were abandoned. This document explains the choices behind the fourth attempt._

## The shape of the problem

Hot-reloading is a small state machine that is awkward because its three steps have different threading rules:

| Step | Where it must run | Why |
| --- | --- | --- |
| Notice a file changed | any thread (the watcher's) | the OS decides when |
| Rebuild + reload | any thread | it is slow, the main thread must not stall |
| Hot-swap | **main thread only** | `IAssetTranscoder.HotSwap` says so (it touches GPU resources) |

So the object has to hop threads twice, and along the way we must never leave a lease behind in the
`AssetCache`, never dangle a reference, and never let a failure damage an asset that is already loaded.

## The central decision: the main thread owns all the state

The single idea that made this version simpler than the previous three is that **only the main thread owns
mutable state machine state**. `Update()` is the only place where work advances a step:

```
Update()
  ├─ MarkStaleAssets()  drain the file event queue      → which assets are stale?
  ├─ StartReloads()     lease + Task.Run per stale asset → thread pool does the slow part
  └─ FinishReloads()    for every completed task: hot-swap, then return the lease
```

The background tasks are pure: they take an input, produce a `ReloadedAsset`, and touch nothing else. All the
bookkeeping (which asset is stale, which rebuild is running, which lease is held) lives in main-thread-only
fields. That is why there is only one lock in the class, and it guards only the two collections that `Track`
(any thread) genuinely shares with `Update`.

The earlier attempts inverted this: the background task pushed results into a concurrent queue *and* returned
its own lease *and* was responsible for its own error handling. That spread the ownership of a lease across
two threads, which is exactly where V2's `// TODO: what happens to the lease?` came from.

## Why I did not serialize to one asset at a time

You offered to let me handle one asset at a time and suspected it would make things *more* complex. It would.
Running rebuilds in parallel is the naturally simple option here:

- **Parallel** needs one dictionary, `InFlight: AssetId → Task<ReloadedAsset>`. Starting work is
  `InFlight.Add(id, task)`, finishing it is "is the task completed?".
- **Serial** needs that *plus* a queue of assets waiting for their turn, plus a "am I currently busy?" flag,
  plus a rule for what to do when the file of a queued asset changes again while it waits.

Serial only removes concurrency I never had to reason about anyway, because the tasks share nothing.

## The lease protocol

The cache is the authority on whether an asset is still alive, so a rebuild has to hold a lease for its whole
duration. The invariant I settled on is:

> **`TryStartReload` takes exactly one lease, and `FinishReload` returns exactly one lease, in a `finally`.**

To make that hold, the lease is taken **synchronously on the main thread before the task is started**, not
inside the task:

```csharp
// TrackedAsset<TAsset, TSettings>
if (!cache.TryLease<TAsset>(Id, out var live)) { reload = null; return false; }
reload = Task.Run(() => Reload(live, fileSystem));
```

This buys two things. The manager always knows whether a lease exists (no "did the task get one before it
threw?" question), and the task receives the live instance as a plain argument, so it never has to look at
shared state. A failed `TryLease` is not an error, it is how we learn that nobody uses the asset anymore,
which is also the moment we stop tracking it.

Because `AssetCache.Return` takes only an `AssetId`, the manager can return the lease without knowing the
asset's type. That is what keeps the type erasure cheap.

## Type erasure

As you predicted in the shell, this needs an abstract non-generic base plus a generic subclass:

- `TrackedAsset` — `AssetId` + `Dependencies` + `TryStartReload(...)`. This is what lives in the dictionaries.
- `TrackedAsset<TAsset, TSettings>` — adds the `TSettings` and the `IAssetTranscoder<TAsset, TSettings>`, and
  is the only place that knows the real types.
- `ReloadedAsset` — the non-generic result: the new dependency list plus an `Action` that performs the swap.
  The generic types are captured inside that closure, so the main thread can run the swap without knowing them.

I deliberately did **not** store the whole `AssetBuildMetaData` on the tracked asset. Only `Settings` and
`Dependencies` are ever used again; the transcoder id and version are already fixed by the transcoder instance.

## Debouncing

An editor saving a file produces several change events (truncate, write, flush). Rebuilding on the first one
means reading a half-written file. So a rebuild only starts when nothing relevant changed for
`Debounce` (default 0.5s).

Two details worth noting:

- The timer is only reset by changes to files that some tracked asset actually depends on. Otherwise the asset
  pipeline writing its own `.cka` build files would keep postponing rebuilds forever.
- The window is global rather than per-asset. Saving file A delays a pending rebuild of unrelated asset B by
  half a second. For a development-only feature that is invisible, and a per-asset timestamp would mean another
  dictionary.

The constructor takes an optional `debounce` so tests can pass `TimeSpan.Zero` and stay deterministic instead
of sleeping. `Update` compares with `>=` precisely so that zero means "no debounce" and never depends on a
timer tick landing.

## Failure handling

The guarantee is that a failure never invalidates a live asset. That falls out of the design almost for free,
because nothing is mutated until the very last step:

- **Rebuild/reload fails** → the task faults, we log it, the live asset was never touched. Its old contents
  stay correct and the next file change tries again.
- **Hot-swap fails** → the transcoder made whatever it made of the instance; we can only log it. Throwing here
  would take the game down over a development feature, which is the wrong trade.
- **Either way** → the `finally` returns the lease, so a broken shader can never leak cache entries.

The rebuild writes into a `MemoryStream` instead of over the `.cka` file on disk, because another thread may
be reading that exact file to load the same asset. The cost is that the on-disk build stays stale after a hot
reload, so the next startup rebuilds the asset once. That seemed clearly better than corrupting a concurrent load.

## Dispose

`Dispose` stops the watcher, drops everything not yet started, and then **waits for the running rebuilds and
finishes them through the normal path** rather than abandoning them. That is not just tidiness: an abandoned
rebuild holds a lease (the cache would report leaked entries) and owns a freshly built asset whose native
resources are only cleaned up by `HotSwap`. Draining and completing them normally handles both, and reuses
`FinishReloads` verbatim.

## Locking

There is one lock, and the rules are:

1. It guards `Tracked` and `Dependents` only — the two collections `Track` shares with `Update`.
2. **The hot-swap runs outside of it.** `HotSwap` is user code that may load another asset, which would take
   the asset manager's `RequestLock` and then this lock. Holding this lock during the swap would be a real
   deadlock, not a theoretical one.
3. Lock order in the system is `AssetManager.RequestLock → HotReloadManager.Lock → AssetCache.Lock`, and
   nothing acquires them in the other direction, so there is no cycle.

## Things I noticed while doing this

Two of these will bite when you wire V3 into `AssetManager`:

1. **`AssetManager.Dispose` disposes in the wrong order.** It calls `Cache.Dispose()` *before*
   `HotReloadManager.Dispose()`. Since the hot reload manager can hold leases, the cache will throw
   "will leak N entries", and the manager's `Cache.Return` will then throw `ObjectDisposedException`. The hot
   reload manager must be disposed first. (`HotReloadManagerV3Tests` declares `cache` before `sut` so that C#'s
   reverse disposal order gets this right, and the leak check then doubles as a lease-accounting assertion.)
2. **`AssetDecoder.Decode` checks that the `.cka` file exists even when you pass a stream override.** It works
   today only because a loaded asset always has a build on disk. It is a trap for any future in-memory-only path.
3. **`HotReloadManagerV2.cs` did not compile** — it calls `RegisterFileDependencies`, which was never written.
   That is committed in `HEAD`, so the branch did not build. I commented the call out with a `TODO` so I could
   verify V3; deleting the abandoned experiments is your call.

## What I left out

- No cancellation of in-flight rebuilds. Saving a file twice quickly just rebuilds twice; the second result
  wins because the swaps are ordered by the main thread.
- No coalescing of a rebuild with a concurrent first-time load of the same asset. The lease makes it safe, only
  wasteful, and it is rare.
- Dead tracked assets are only cleaned up when a file change reveals that the cache dropped them. An asset that
  is unloaded and never touched again leaves a small entry behind until then.
