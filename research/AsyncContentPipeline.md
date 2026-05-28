# Async Content Pipeline via `System.Threading.Channels`

> **Note:** This may be relevant for the content manager (still to be designed). The pattern below describes a clean producer/consumer seam between async asset loading and the synchronous render thread, which is exactly the kind of boundary a content manager will need.

`System.Threading.Channels` is .NET's built-in producer/consumer queue, in `System.Threading.Channels`. Think of it as a `BlockingCollection<T>` redesigned for the `async`/`await` era: thread-safe by construction, lock-free in the common path, supports both sync and async reads/writes, and signals completion explicitly.

## Why it fits a game engine well

A channel cleanly separates two threads with different rules:

- **Producer**: a loader doing async I/O. Doesn't touch D3D's immediate context. Can run on the thread pool.
- **Consumer**: the render thread. Drains the channel each frame using a non-blocking `TryRead`, does the GPU upload, moves on.

The producer never blocks the render loop; the render loop never blocks waiting for I/O. The channel is the seam.

## Shape of the data

Don't push D3D objects through the channel — that defeats the point. Push CPU-side payloads ready for the render thread to upload:

```csharp
internal sealed record LoadedShader(FilePath Path, string Source);
internal sealed record LoadedMesh(FilePath Path, byte[] Vertices, byte[] Indices);

// Or a discriminated-union-ish base if you want one channel for many asset types
internal abstract record LoadedAsset;
```

## Creating the channel

```csharp
private readonly Channel<LoadedAsset> loaded =
    Channel.CreateUnbounded<LoadedAsset>(new UnboundedChannelOptions
    {
        SingleReader = true,   // only the render thread reads
        SingleWriter = false,  // multiple loader tasks may write
    });
```

`SingleReader = true` lets the channel use a faster internal path. Use `CreateBounded<T>(capacity)` instead to get backpressure (producers wait when the queue is full).

## Producer side — the loader

```csharp
public Task QueueShaderLoad(FilePath path) => Task.Run(async () =>
{
    var source = await FileSystem.ReadAllText(path);
    await loaded.Writer.WriteAsync(new LoadedShader(path, source));
});
```

`Task.Run` gets the work off the calling thread and into the thread pool. Everything inside happens off the render thread. `WriteAsync` only awaits if the channel is bounded and full — for an unbounded channel it completes synchronously.

Many of these can be in flight; the channel handles concurrency.

## Consumer side — drain in the loop

```csharp
public void Run()
{
    while (running)
    {
        ImGuiController.NewFrame((float)elapsed);
        HandleInput();
        HandleResize();

        DrainLoadedAssets();   // <-- new

        SwapChain.Clear(Device.ImmediateDeviceContext);
        // ...rest of frame...
    }
}

private void DrainLoadedAssets()
{
    while (loaded.Reader.TryRead(out var asset))
    {
        switch (asset)
        {
            case LoadedShader s:
                var vs = ShaderCompiler.CompileVertexShader(FileSystem, s.Source, ...);
                // GPU upload via ImmediateDeviceContext is fine here — we're on the render thread
                break;

            case LoadedMesh m:
                // upload vertex/index buffers via the immediate context
                break;
        }
    }
}
```

`TryRead` is **non-blocking** — returns false if the queue is empty. That's the key: the render loop never stalls. If nothing's ready this frame, it just moves on.

## Shutdown

```csharp
loaded.Writer.Complete();   // no more writes will be accepted
```

After completion, `TryRead` still drains anything left, then returns false forever. Useful for clean teardown.

## Why this beats rolling a custom queue

- No `lock`s in your code.
- Backpressure is built in (`CreateBounded`).
- `ReadAllAsync` gives you an `IAsyncEnumerable<T>` for free if you ever want a dedicated consumer task.
- Allocations are minimal — the channel reuses internal nodes.

## One subtle point

A channel does not *create* a thread — it just routes between threads that already exist. The producer threads come from `Task.Run` (or wherever the async work is started); the consumer thread is the existing render thread. The channel is just the handoff mechanism.

For this engine that means the addition is small: one `Channel<LoadedAsset>` field, one `DrainLoadedAssets` call per frame, and a couple of `QueueXLoad` methods on the loader side. Everything else stays the same.
