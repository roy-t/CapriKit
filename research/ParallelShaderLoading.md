# Parallel Shader Loading via `Parallel.ForEachAsync`

> **Note:** Companion to `AsyncContentPipeline.md`. That note covers the producer/consumer *seam* (a `Channel` drained by the render thread). This note covers the *producer* side: how to compile a finite batch of shaders in parallel without standing up a dedicated thread pool, and without ever blocking the render loop.

## When to reach for this

For a **finite, known batch** ("compile these N shaders, then we're done"), dedicated long-lived worker threads are overkill — they earn their keep only when workers persist across frames (a standing job system). For a one-shot burst, `Parallel.ForEachAsync` gives you the same parallelism with zero thread lifecycle code, and a natural "batch complete" signal for free.

## The core idea

Split the work along the seam the engine already has:

- **Compile** (`ShaderCompiler.CompileVertexShader` → `VertexShaderByteCode`) is device-free, thread-safe CPU work. Run it in parallel, off the render thread.
- **Create** (`ShaderCompiler.CreateVertexShader` → `IVertexShader`) touches `ID3D11Device`. The device is free-threaded, but we keep this on the render thread anyway so all GPU-facing work lives in one place.

The `Channel` carries the CPU-side `VertexShaderByteCode` across the seam.

## `Parallel.ForEachAsync` is synchronous to its *caller*

`Parallel.ForEach`/`ForEachAsync` does not return until every iteration finishes. That only matters for the thread that calls it — so we never call it on the render thread. Results are written into the channel *from inside the loop body*, so the render thread sees them stream in frame by frame; it does not wait for the whole batch.

## The loader

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using CapriKit.DirectX11.Resources.Shaders;
using CapriKit.IO;

internal sealed class ShaderLoader
{
    // A bare source string can't be compiled or identified on the way out,
    // so carry the name + entry point with it.
    public readonly record struct Request(string Name, string EntryPoint, string Source);

    private readonly Channel<VertexShaderByteCode> results =
        Channel.CreateUnbounded<VertexShaderByteCode>(new UnboundedChannelOptions
        {
            SingleReader = true,    // only the render thread drains
            SingleWriter = false,   // many compile tasks write
        });

    // Fire-and-forget from the render thread: returns instantly, work runs on the pool.
    public Task CompileAll(IReadOnlyVirtualFileSystem fileSystem, DirectoryPath includePath, Request[] requests, CancellationToken ct = default)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = ct,
        };

        return Parallel.ForEachAsync(requests, options, async (request, token) =>
        {
            var byteCode = ShaderCompiler.CompileVertexShader(
                fileSystem, includePath, request.Source, request.EntryPoint, request.Name);

            await results.Writer.WriteAsync(byteCode, token);
        })
        .ContinueWith(_ => results.Writer.Complete(), TaskScheduler.Default);
    }

    // Render thread calls this each frame; non-blocking.
    public bool TryDequeue([NotNullWhen(true)] out VertexShaderByteCode? byteCode)
        => results.Reader.TryRead(out byteCode);
}
```

## Starting it

Do not `await` it on the render thread, but *do* observe the task so exceptions don't vanish:

```csharp
var loading = shaderLoader.CompileAll(fileSystem, includePath, requests);
_ = loading.ContinueWith(t => Log(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
```

## Draining on the render thread

This is where the **device** call happens, safely on the render thread:

```csharp
private void DrainCompiledShaders()
{
    while (shaderLoader.TryDequeue(out var byteCode))
    {
        var shader = ShaderCompiler.CreateVertexShader(byteCode, Device); // device call: render thread
        // store/use `shader`
    }
}
```

Call `DrainCompiledShaders()` once per frame, where `AsyncContentPipeline.md` put `DrainLoadedAssets()`.

## Why it works

- **The compile inside the async body is synchronous CPU work, and that's fine.** `CompileVertexShader` doesn't `await` anything; it runs to completion on whichever pool thread `ForEachAsync` assigned. Because `ForEachAsync` runs up to `MaxDegreeOfParallelism` of these concurrently, you still get real parallel compilation. For purely in-memory sources, plain `Parallel.ForEach` would be equally valid — `ForEachAsync` becomes the right tool the moment a file read enters the body (see below).
- **`WriteAsync` completes synchronously here.** The channel is unbounded, so `WriteAsync` never actually suspends — same practical cost as `TryWrite`, but it throws cleanly if the channel is closed instead of silently returning `false`.
- **`Complete()` on the continuation gives a real "batch done" signal.** Once all requests finish, the channel completes; `TryDequeue` drains the remainder, then returns `false` forever. That's the edge to flip a loading-screen flag on.

## Where `ForEachAsync` earns the "Async"

If the input is **paths** rather than in-memory source, the body becomes I/O-bound and the `await` stops being trivial — workers overlap disk reads with other workers' CPU compiles:

```csharp
return Parallel.ForEachAsync(paths, options, async (path, token) =>
{
    var source   = await fileSystem.ReadAllTextAsync(path, token);   // real await: disk I/O
    var byteCode = ShaderCompiler.CompileVertexShader(fileSystem, includePath, source, entryPoint, name);
    await results.Writer.WriteAsync(byteCode, token);
});
```

That is the case where `ForEachAsync` clearly beats `Parallel.ForEach`.
