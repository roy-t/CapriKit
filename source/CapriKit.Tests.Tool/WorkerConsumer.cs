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
internal record Job<T>(string Id, Func<Task<T>> Work);

internal sealed class TestScreenLoader
{
    private readonly Channel<Loaded<ITestScreen>> Work;

    public TestScreenLoader()
    {
        Work = Channel.CreateUnbounded<Loaded<ITestScreen>>(new()
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void StartWork(params IReadOnlyList<Job<ITestScreen>> jobs)
    {
        Parallel.ForEachAsync(jobs, async (job, token) =>
        {
            try
            {
                var screen = await job.Work();
                await Work.Writer.WriteAsync(new Loaded<ITestScreen>(job.Id, screen, null));
            }
            catch (Exception ex)
            {
                await Work.Writer.WriteAsync(new Loaded<ITestScreen>(job.Id, null, ex));
            }
        }).FireAndForget(ex => Debugger.Log(1, "Task", ex.Message), OnCompleted);
    }

    private void OnCompleted()
    {
        Work.Writer.Complete();
    }

    public bool TryDequeue([NotNullWhen(true)] out Loaded<ITestScreen>? screen)
    {
        return Work.Reader.TryRead(out screen);
    }
}
