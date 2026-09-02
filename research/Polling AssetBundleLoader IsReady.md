# Polling AssetBundleLoader.IsReady

_Written 2026-08-20, while going through `CapriKit.AssetPipeline` on the `feature/asset_pipeline` branch. We
had just finished the `AssetPool` tests and the fix for `PutOrLease` handing out a disposed asset when the
same instance is put twice. The question that came up next: `AssetBundleLoader<TBundle>.IsReady` is polled
every frame and walks all its handles, and bundles are expected to hold around 40 assets._

## What the current code costs

```csharp
foreach (var handle in handles)
{
    if (!handle.IsResolved) { value = default; return false; }
}
```

The loop already stops at the **first** unresolved handle, so it never walks the whole bundle unless the
bundle is nearly done. What it repeats every frame is the prefix of handles that resolved earlier: a
pointer dereference plus a volatile bool read each, over a `List<AssetHandle>` of references that are
scattered across the heap.

Worst case per poll is therefore ~40 dependent loads, so tens of nanoseconds (an estimate from the shape of
the loop, not a measurement). At 60 Hz with a handful of bundles in flight that is microseconds per second.

**So none of the proposals below are a rescue of a hot loop.** Take one because it is simpler than what is there
now, or because it gives you the progress information the `// TODO` in `AssetBundleLoader.cs` asks for.

## The invariant all three proposals lean on

`AssetHandle.Resolve` asserts it is only called once and only ever flips `isResolved` from false to true.
A handle that is resolved stays resolved. "Resolved" is monotone, which is what makes it safe to remember
progress between polls without any notification from the handle side.

## Proposal A: BitArray plus a resume index

The idea as originally sketched: keep one bit per handle and resume from the first handle that was not
ready last time. Written out, and extended so the bits actually earn their keep by also counting progress:

```csharp
private readonly BitArray resolved = new(handles.Count);
private int resolvedCount;
private int cursor; // every handle before this one is resolved

public int Total => handles.Count;
public int Loaded => resolvedCount;

public bool IsReady([NotNullWhen(true)] out TBundle? value)
{
    if (isReady) { value = result!; return true; }

    // Only look at handles that were not resolved yet, the bits let us skip the ones that were.
    for (var i = cursor; i < handles.Count; i++)
    {
        if (resolved[i] || !handles[i].IsResolved) { continue; }

        resolved[i] = true;
        resolvedCount++;
    }

    // Everything up to the cursor is resolved, so the next poll can start there.
    while (cursor < handles.Count && resolved[cursor]) { cursor++; }

    if (resolvedCount < handles.Count) { value = default; return false; }

    result = factory(new AssetHandleResolver(this));
    isReady = true;
    value = result;
    return true;
}
```

Cost per poll: one bit read per handle that resolved out of order, one volatile read per handle that is
still pending. Both shrink as the bundle loads. Total work over the bundle's life is O(n) plus one pass
over the shrinking tail per poll.

Notes:
- A `bool[]` is the better container at this size: 40 bytes instead of 8, but no masking and no extra type.
  `BitArray` only starts paying off in the thousands of handles.
- Without the counting you can drop the bits entirely, see proposal B for why.
- If you want progress but not the extra arrays, see proposal C. It shrinks the list instead of the scan
  range, and it ended up dominating this proposal.

## Proposal B: resume cursor only (recommended for a yes/no IsReady)

If `IsReady` stays a yes/no question, one integer replaces the whole thing:

```csharp
private int cursor; // first handle that was not resolved yet

public bool IsReady([NotNullWhen(true)] out TBundle? value)
{
    if (isReady) { value = result!; return true; }

    // Handles never go back to unresolved, so everything before the cursor stays resolved and
    // each poll only looks at the handles that were still pending during the previous poll.
    while (cursor < handles.Count && handles[cursor].IsResolved) { cursor++; }

    if (cursor < handles.Count) { value = default; return false; }

    result = factory(new AssetHandleResolver(this));
    isReady = true;
    value = result;
    return true;
}
```

This is a two line change to the current implementation and it is *less* code than proposal A, not more.

Cost per poll: one comparison plus one volatile read per handle that resolved since the previous poll.
Summed over the bundle's life that is n reads plus one comparison per poll, which is as good as it gets
without the handles pushing.

**Why the bits drop out.** Once you also keep a cursor, the loop returns at the first unresolved handle, so
it never looks past it, so it never sets a bit past it. The bits are therefore always "set below the
cursor, clear at and above it", which is exactly what the cursor already says. The BitArray is redundant
unless something makes you keep scanning past the first unresolved handle, and only progress counting does
that. That is the whole difference between A and B.

**Progress.** `cursor / (float)handles.Count` is a lower bound: monotone and never jumps backwards, so it
drives a progress bar fine, but it under-reports while an early asset is slow. An accurate `37 of 40`
needs proposal C.

## Proposal C: shrink the pending list

Instead of remembering where to resume, throw away what is done. The loader keeps its own list of handles
it is still waiting on and swap-removes each one as it resolves:

```csharp
// A copy: the builder still owns the list it handed us.
private readonly List<AssetHandle> pending = [.. handles];
private readonly int total = handles.Count;

public int Total => total;
public int Loaded => total - pending.Count;
public IReadOnlyList<AssetHandle> Pending => pending;

public bool IsReady([NotNullWhen(true)] out TBundle? value)
{
    if (isReady) { value = result!; return true; }

    // Backwards, so that swapping the last handle into the gap cannot skip a handle we still have to see.
    for (var i = pending.Count - 1; i >= 0; i--)
    {
        if (!pending[i].IsResolved) { continue; }

        pending[i] = pending[^1];
        pending.RemoveAt(pending.Count - 1);
    }

    if (pending.Count > 0) { value = default; return false; }

    result = factory(new AssetHandleResolver(this));
    isReady = true;
    value = result;
    return true;
}
```

Cost per poll: exactly one volatile read per handle that is still pending, and the scan shrinks as the
bundle loads. That is the price of an exact count. Proposal B can stop at the first unresolved handle
because it only has to answer yes or no, this one has to look at all of them to know how many arrived.

Notes:
- **The copy is required.** `AssetBundleBuilder.Build` passes its own `Handles` list straight into the
  constructor, so compacting it in place would corrupt a second bundle built from the same builder.
  (`Build` already re-points every `handle.Owner`, so building twice is not really supported today, but
  this proposal should not be the thing that breaks it.)
- Mentioning `handles` only in field initialisers means the primary constructor does not capture it, so
  after the copy the loader no longer keeps the builder's list alive.
- It is the only variant that can say *what* it is waiting on: `pending` is exactly the set of assets that
  have not arrived, which is what a loading screen or a debug overlay wants to show.
- The swap-remove destroys the order. Nothing reads it today (`AssetHandleResolver` goes through the handle,
  not through this list). If that changes, walk forwards and compact with a write index instead, same cost.
- Keeping the resolved handles in a second list gives the same progress number for another allocation and
  an `Add` per handle. Nothing in the pipeline consumes them, so I would leave that second list out until
  something does.
## Which one

| | A: bits + cursor | B: cursor | C: shrinking list |
|---|---|---|---|
| Progress | exact count | lower bound | exact count |
| Says what is still pending | no | no | yes |
| Extra state | `bool[]` + count + cursor | cursor | list copy |
| Allocates per bundle | `bool[n]` | nothing | list of n references |
| Reads per poll | one per pending handle, plus a bit per handle that resolved out of order | one while blocked, then one per newly resolved handle | one per pending handle |
| Lines vs. today | ~+12 | ~+2 (and one `foreach` removed) | ~+6 |

Go with **B** while `IsReady` stays a yes/no question, and with **C** as soon as you want a real progress
number. B is by far the cheapest to poll: while it is blocked on one handle it reads that one handle and
returns, where A and C have to look at everything still pending to keep their count exact.

**C ended up dominating A**, which is worth writing down since A is where this started. It has fewer moving
parts, it never rescans the resolved-but-out-of-order handles that A's bits exist to skip, and it hands you
the pending set for free. A's only edge is that it leaves the handle list alone, which buys about 320 bytes
per bundle. A stays in this document for the reasoning, not as a candidate.

Optional add-on, composable with any of them: let `AssetManager` bump a `ResolveGeneration` counter whenever
`Update` resolves anything, and have the loader remember the generation it last saw. A poll on a frame
where nothing resolved at all then returns false after a single int comparison, for every bundle at once.
Costs the loader a reference to the manager, so only worth it if many bundles are in flight.

## Considered and rejected: let the handles push

The obvious O(1) answer is a countdown that `AssetHandle.Resolve` decrements through the `Owner` it already
carries:

```csharp
internal void Resolve(object asset)
{
    value = asset;
    isResolved = true;
    Owner?.OnHandleResolved(); // remaining--
}
```

This does not survive contact with the loading path, for two reasons:

1. `AssetManager.Load` resolves straight from the cache while `AssetBundleBuilder.Load` is still running,
   which is *before* `AssetBundleBuilder.Build` assigns `handle.Owner`. Every cache hit is a lost
   decrement. `Build` would have to count the handles that are already resolved and seed the countdown.
2. That seeding then races: `CreateBundle`, `Load` and `Build` are documented as callable from any thread,
   while `Resolve` runs on the main thread inside `Update`. A handle can resolve between `Build` reading
   `handle.IsResolved` and `Build` writing `handle.Owner`, which either loses the decrement or counts it
   twice.

Closing that means assigning `Owner` and seeding the count under the `AssetManager.RequestLock`, so the
bundle builder starts depending on the manager's lock and the loading path grows a lock section, all to
save the ~40 bool reads per frame we started with. This is the complexity that was rightly suspected up
front, and the reason all three proposals above stay on the polling side.

## Related TODOs in the same file

- `// TODO: how can we put a sort of progress bar and progress information on this thing?` is exactly the fork above:
  B if a lower bound that never jumps backwards is enough, C if you want to show a real count.
- `// TODO: Add a method to block and wait without eating all the CPU` is a different problem. None of the
  proposals help: blocking needs something to wait on, and the only thing that could signal it is the main
  thread inside `AssetManager.Update`, so it would have to be a `ManualResetEventSlim` (or a
  `TaskCompletionSource`) that `Update` sets once the bundle's last handle resolved. Worth its own note.
