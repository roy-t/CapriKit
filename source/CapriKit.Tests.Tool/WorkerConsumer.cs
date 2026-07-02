using CapriKit.Concurrency.Async;
using CapriKit.Tests.Tool.Tests.Framework;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace CapriKit.Tests.Tool;

// Prototype to see how to do async/multi-threaded loading work.
// Remember that ID3D11Device is free threaded (unless if we create it with D3D11_CREATE_DEVICE_SINGLETHREADED)
// things that require a ID3D11Context are not!
// (Each thread can have its own deferred context and sync with the main threads immediate context)

internal record Loaded<T>(string Id, T? Item, Exception? Exception);
internal record Job<T>(string Id, Func<CancellationToken, Task<T>> Work);

internal sealed class TestScreenLoader : IDisposable
{
    private readonly Channel<Loaded<ITestScreen>> Work;
    private readonly CancellationTokenSource Cancellation;

    public TestScreenLoader()
    {
        Work = Channel.CreateUnbounded<Loaded<ITestScreen>>(new()
        {
            SingleReader = true,
            SingleWriter = false
        });
        Cancellation = new CancellationTokenSource();
    }

    public void StartWork(params IReadOnlyList<Job<ITestScreen>> jobs)
    {
        Parallel.ForEachAsync(jobs, Cancellation.Token, async (job, token) =>
        {
            try
            {
                var screen = await job.Work(token);
                await Work.Writer.WriteAsync(new Loaded<ITestScreen>(job.Id, screen, null));
            }
            catch (OperationCanceledException)
            {
                // A cancelled job is not a failure and has nothing to publish
            }
            catch (Exception ex)
            {
                await Work.Writer.WriteAsync(new Loaded<ITestScreen>(job.Id, null, ex));
            }
        }).FireAndForget(ex => Debugger.Log(1, "Task", ex.Message), OnCompleted);
    }

    /// <summary>
    /// Prevents queued jobs from starting and signals in-flight jobs to stop
    /// </summary>
    public void Cancel()
    {
        Cancellation.Cancel();
    }

    private void OnCompleted()
    {
        Work.Writer.Complete();
    }

    public bool TryDequeue([NotNullWhen(true)] out Loaded<ITestScreen>? screen)
    {
        return Work.Reader.TryRead(out screen);
    }

    /// <summary>
    /// Blocks until all in-flight jobs have finished and disposes any screens that were never dequeued
    /// </summary>
    public void DrainAndDisposeRemaining()
    {
        DrainAndDisposeRemainingAsync().GetAwaiter().GetResult();
    }

    private async Task DrainAndDisposeRemainingAsync()
    {
        // ReadAllAsync completes once OnCompleted has marked the writer as complete
        await foreach (var loaded in Work.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            loaded.Item?.Dispose();
        }
    }

    public void Dispose()
    {
        Cancellation.Dispose();
    }
}
