# Asset Pipeline — Loading Groups

Status: sketch for discussion, 2026-08-10. Companion to `AssetPipelineArchitecture.md`.

## Context

While working on `source/CapriKit.AssetPipeline/vNext/` — the threading-aware rewrite. `AssetCache`
was already done (thread-safe lease/return with deferred disposal); the open question was how
assets get *delivered* now that `Load` no longer returns a `Task` to gameplay code.

The concrete worry: in the old engine a `Car` took its texture, model and shader through
constructor injection. With an optimistic (`TryLoad`) or channel-based model, every system would
seem to need a "do I have everything yet?" check in `Update`, every frame. That is the thing this
document is trying to avoid.

## The framing

The question isn't *task vs channel vs optimistic*. It's **who is allowed to exist before their
assets exist**. Engines don't answer that uniformly — they split the world:

- **Engine assets** — lighting/shadow shaders, BRDF LUT, blue noise, error textures. Part of the
  engine build, not content. Must exist or the engine is broken. Loaded in a boot phase *before*
  any system is constructed. Constructor injection, no nulls, no polling.
- **Content assets** — levels, entities, characters. Their consumers have to tolerate absence
  anyway (streaming, LOD, missing file, hot reload), so absence is designed in rather than bolted on.

Source calls this `Precache*` at level load — requesting a non-precached asset at runtime is a hard
error in dev builds. Unreal splits `FStreamableManager` requests from always-loaded cooked packages.

The rule that falls out: **never expose "the asset might not be here" to gameplay code.** That is
precisely what forces a per-frame check into every system. `TryLoad` is fine as an internal
cache-hit fast path; it is a trap as the public API.

## Options considered

| Option | Shape | Good for | Bad for |
|---|---|---|---|
| **A** Resolve-then-construct | Boot phase batch-loads declared requirements in parallel, systems are constructed from a resolved lookup that cannot fail | Lighting, Shadowing, engine systems | Anything streamed — loading the whole game up front |
| **B** Placeholder + `HotSwap` | `Load` returns a usable instance immediately whose contents are a placeholder; swapped in place when the payload lands | Assets whose absence only affects pixels | Simulation data (placeholder collision mesh = fall through floor), and **shaders** — see below |
| **C** Group barrier + completion channel | Interlocked counter per group; the worker that drives it to zero pushes the group onto a channel the main thread drains | Levels, entities, anything with N dependencies | Nothing much — it is the general mechanism |
| **D** Async confined to load scopes | Workers `await` N assets, construct the finished object, hand it to the sim through one channel | Level loading, streaming coordinators | — |

Tasks were never the mistake; handing a `Task` to *gameplay* was, because async then infects
everything upward. In D, `Car`'s constructor still takes its assets directly — it just isn't the
main thread calling it.

**Recommended split for CapriKit:** A for engine systems, B for renderer content, C+D for levels
and entities.

## Why shaders can't use placeholders (Option B)

A placeholder is a valid substitute only when **every instance of the asset type shares one
interface**. A texture qualifies: an SRV is an SRV, the consumer doesn't care what pixels are
behind it. Content varies, interface doesn't.

For a shader the interface *is* the identity:

- `CreateInputLayout` validates the `InputElementDescription[]` against the VS input signature in the blob
- cbuffer register assignments (`b0`, `b1`, …) and their field layouts
- SRV/sampler slots (`t0`, `s0`)

A placeholder VS substitutes for a real VS only if it has the same input signature *and* binding
layout — at which point it is a hand-written stub per shader, not a placeholder.

### The same gap is a latent hot-reload bug

`IVertexShader.HotSwap` (`source/CapriKit.DirectX11/Resources/Shaders/VertexShader.cs:12`) swaps
`Blob` and `ID3D11VertexShader`. But the `IInputLayout` was created from the *old* blob and is owned
by the consumer — e.g. `ImGuiEffect.cs:24` holds it in a separate field. Nothing tells that layout
its shader changed.

D3D11 won't crash: `CreateInputLayout` validates at creation and the layout is independent
afterwards. But reload a shader whose signature grew a `TEXCOORD1` and the old layout no longer
feeds it — debug-layer complaint, zero/garbage in that register. Subtly wrong, which is worse.

`HotSwap` on a bare vertex shader is incoherent *because* a bare vertex shader isn't independently
usable — the same premise as the placeholder problem.

### Fix: the effect is the atomic asset

The transcoder's `TAsset` should be the smallest **self-consistent** unit — the thing owning a
complete, valid interface:

```csharp
internal sealed class Effect   // IAssetTranscoder<Effect, TSettings> produces this
{
    public ID3D11VertexShader Vertex { get; private set; }
    public ID3D11PixelShader Pixel { get; private set; }
    public IInputLayout InputLayout { get; private set; }  // built from *this* blob, at decode

    // snip
}

public override void HotSwap(Effect instance, Effect newParts)
{
    var old = instance.Exchange(newParts);   // shader + layout swap together, main thread
    // snip: dispose old
}
```

A mismatched (blob, layout) pair becomes unrepresentable — the layout is created inside `Decode`
from the blob being decoded. `CapriKit.Generators.HLSL` already emits the input element
descriptions as compile-time constants, so this construction is deterministic.

Shaders then belong in **category A**: they're a few KB, the set is closed at compile time (the
generator enumerates every `#pragma VertexShader` / `#pragma PixelShader` entry point, so the boot
manifest can be generated), and a renderer missing its lighting shader isn't degraded, it's
non-functional.

For content-driven materials the answer is **skip the draw**, not substitute: the renderable isn't
registered with the render system until its effect exists. Nothing to branch on in the hot loop —
the object simply isn't in the visible set. That's UE5's PSO-precache behaviour, and it composes
with the group barrier below.

A real shader fallback is achievable but only under a convention: fix the binding layout
engine-wide (per-frame `b0`, per-view `b1`, per-object `b2`) and standardize vertex formats. Then a
magenta error shader with a matching input signature is a genuine drop-in — that is exactly why
Unity's error shader works, it demands only the minimal interface. Worth building for the
**broken/corrupt** case so a bad edit doesn't kill the frame; not worth it for the not-yet-loaded
case, which boot-loading eliminates.

## Option C sketch — typed loading groups

Assumption: requirements are hardcoded in C# for the foreseeable future.

### The type-safety trick

Make "not loaded yet" **unrepresentable** in the consumer's types. Three types do it:

- `Ticket<TAsset>` — a claim check with *no value accessor at all*
- `ResolvedAssets` — the only way to redeem a ticket, obtainable only after the group completes
- `AssetGroup<TResult>` — produces one strongly-typed result whose fields are plain non-nullable assets

No `IsLoaded`, no `.Value` that can be read early, and no arity explosion
(`AssetGroup<T1, T2, T3, …>`) because tickets carry their type individually and a closure
reassembles them.

### Declaration side

```csharp
public abstract class Ticket
{
    internal AssetId Id { get; init; }
    internal object? Asset;   // written by loader, read on main thread — the one erasure point
}

/// <summary>A claim check for one asset. Redeem with <see cref="ResolvedAssets.Get"/>.</summary>
public sealed class Ticket<TAsset> : Ticket where TAsset : class;

public readonly struct ResolvedAssets
{
    // Safe by construction: the ticket's loader produced exactly TAsset
    public TAsset Get<TAsset>(Ticket<TAsset> ticket) where TAsset : class
        => (TAsset)ticket.Asset!;
}

public sealed class AssetGroupBuilder
{
    public Ticket<TAsset> Add<TAsset, TSettings>(
        AssetId id, IAssetTranscoder<TAsset, TSettings> transcoder, TSettings settings)
        where TAsset : class
    {
        var ticket = new Ticket<TAsset> { Id = id };
        // Both type params erased into the closure; TAsset survives on the ticket
        Requests.Add(new Request(ticket, (m, ct) => m.LoadAsync(id, transcoder, settings, ct)));
        return ticket;
    }

    public AssetGroup<TResult> Build<TResult>(Func<ResolvedAssets, TResult> factory) { /* snip */ }
}
```

### A hardcoded requirement list

```csharp
internal sealed class ShadowAssets
{
    public static AssetGroup<ShadowAssets> Define(
        AssetGroupBuilder b, EffectTranscoder effects, TextureTranscoder textures)
    {
        var shadow = b.Add("shaders/shadow.hlsl", effects, EffectSettings.Default);
        var noise  = b.Add("textures/blue-noise.png", textures, TextureSettings.Default);

        return b.Build(r => new ShadowAssets(r.Get(shadow), r.Get(noise)));
    }

    private ShadowAssets(Effect shadow, ITexture2D noise)
    {
        Shadow = shadow;
        Noise = noise;
    }

    public Effect Shadow { get; }      // non-nullable, always valid
    public ITexture2D Noise { get; }
}

// The system never sees the pipeline at all
internal sealed class ShadowSystem(ShadowAssets assets, Device device) { /* snip */ }
```

Adding a requirement is two lines and the compiler forces the constructor to match. Deleting one
breaks the build. That is the payoff for hardcoding requirements.

### The barrier and the drain

```csharp
public abstract class AssetGroup : IDisposable
{
    private int outstanding;

    // Called on whichever worker finished the request
    internal void OnRequestCompleted(ChannelWriter<AssetGroup> ready)
    {
        if (Interlocked.Decrement(ref outstanding) == 0) { ready.TryWrite(this); }
    }

    internal abstract void Materialize();   // main thread only
}

public sealed class AssetGroup<TResult> : AssetGroup
{
    private readonly Func<ResolvedAssets, TResult> Factory;
    private readonly TaskCompletionSource<TResult> Source =
        new(TaskCreationOptions.RunContinuationsAsynchronously);  // don't hijack the main thread

    internal override void Materialize() => Source.SetResult(Factory(new ResolvedAssets()));

    public Task<TResult> Completion => Source.Task;
    public bool TryTake([NotNullWhen(true)] out TResult? value) { /* snip */ }
}
```

```csharp
// AssetManager.Update() — main thread, once per frame
while (ReadyGroups.Reader.TryRead(out var group))
{
    group.Materialize();   // usually zero iterations
}
Cache.Collect();
```

This answers the original worry: the main thread drains **one** channel of finished groups. No
system polls its own assets, and the per-frame cost is O(groups that finished this frame), normally
zero.

One mechanism serves both consumption styles:

```csharp
// Boot / loading screen (Option A)
var group = ShadowAssets.Define(builder, effects, textures);
services.AddSingleton(new ShadowSystem(await group.Completion, device));

// Streaming (Option C) — entity doesn't exist until the group lands
if (pendingLevel.TryTake(out var level)) { World.Install(level); }
```

### Lifetime and failure

Make the **group** the refcount unit, not the individual asset — it lines up with
`AssetCache.Return` and makes level unload a single `Dispose`:

```csharp
public void Dispose()
{
    foreach (var ticket in Tickets)
    {
        if (ticket.Asset is not null) { Cache.Return(ticket.Id); }
    }
}
```

The failure path matters because `AssetCache.Dispose` throws on leaks. If request 4 of 5 throws,
the three that already landed hold leases. Catch per-request, store the exception, let the counter
reach zero anyway, and have `Materialize` fault the group *and* return the partial leases.
Otherwise one bad asset file becomes a leak exception at shutdown that says nothing about the cause.

## Constraints to document for transcoder authors

**Where `Decode` may run.** `Device.cs:21-31` creates the device without
`DeviceCreationFlags.Singlethreaded`, so `ID3D11Device` resource creation is free-threaded —
`CreateVertexShader` / `CreateTexture2D` on a worker is fine. `ID3D11DeviceContext` is **not**. A
transcoder needing `Map` or `UpdateSubresource` must either create immutable resources with initial
data (no context involved) or defer that step to `Materialize` on the main thread. Violating this
produces corruption rather than an exception, so it needs to be an explicit written rule.

**Ticket provenance.** Nothing at compile time stops redeeming a ticket from group A inside group
B's factory. The closure capture makes it awkward in practice; a `Debug.Assert(ticket.Owner == this)`
in `Get` covers the rest cheaply.

## Open questions

- Does `AssetGroup<TResult>` need `Task` at all, or is `TryTake` + a main-thread callback enough?
  `Completion` is convenient for boot code that legitimately awaits on a loading screen.
- Where does hot reload re-enter? A reloaded asset inside a live group needs `HotSwap`, not
  re-materialization — the `TResult` already handed out must keep its identity.
- Should `AssetManager.TryLoad` stay public? Current stub returns `bool` with nowhere to put the
  asset; it wants `out TAsset` with `[NotNullWhen(true)]`, mirroring `AssetCache.TryLease` — and
  arguably it should be internal, as the fast path the three policies sit on.
