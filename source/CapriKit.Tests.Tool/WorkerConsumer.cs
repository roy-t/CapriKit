using CapriKit.Tests.Tool.Tests.Framework;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace CapriKit.Tests.Tool;

// Prototype to see how to do async/multi-threaded loading work.
// Remember that ID3D11Device is free threaded (unless if we create it with D3D11_CREATE_DEVICE_SINGLETHREADED)
// things that require a ID3D11Context are not!
// (Each thread can have its own deferred context and sync with the main threads immediate context)

internal record Loaded<T>(string Id, T? Item, Exception? Exception);
internal record Job<T>(string Id, Func<Task<T>> Work);

// TODO: this would probably all work better with async/await
internal sealed class TestScreenLoader
{
    private readonly BlockingCollection<Job<ITestScreen>> Jobs;
    private readonly Channel<Loaded<ITestScreen>> Work;

    public TestScreenLoader(int concurrency)
    {
        Jobs = new BlockingCollection<Job<ITestScreen>>();
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
        }).Forget();
        // Or should this just be static and return a channel?
        // No need for BlockingCollection now.
    }

    public bool TryDequeue([NotNullWhen(true)] out Loaded<ITestScreen>? screen)
    {
        return Work.Reader.TryRead(out screen);
    }

    //public void Enqueue(string source) => Jobs.Add(source);
    //public void CompleteAdding() => Jobs.CompleteAdding();      // happy-path shutdown
    //public bool TryDequeue([NotNullWhen(true)] out LoadedShader? shader) => Work.Reader.TryRead(out shader);

    //private void Run()// If we want to do async IO we can also do Parallel.ForEachAsync
    //{
    //    foreach (var source in Jobs.GetConsumingEnumerable())  // ends after CompleteAdding + drain
    //    {
    //        try { Work.Writer.TryWrite(DoMagic(source)); }
    //        catch (Exception ex)
    //        {
    //            Work.Writer.Complete(ex); // throws the exception bout only out of ReadAsync/WaitReadAsync
    //        }
    //    }
    //}

    //private static LoadedShader DoMagic(string source)
    //{
    //    return new LoadedShader([]);
    //}
}


public static class TaskExtensions
{
    /// <summary>
    /// Observes the task to avoid the UnobservedTaskException event to be raised.
    /// </summary>
    public static void Forget(this Task task)
    {
        // note: this code is inspired by a tweet from Ben Adams: https://twitter.com/ben_a_adams/status/1045060828700037125
        // Only care about tasks that may fault (not completed) or are faulted,
        // so fast-path for SuccessfullyCompleted and Canceled tasks.
        if (!task.IsCompleted || task.IsFaulted)
        {
            // use "_" (Discard operation) to remove the warning IDE0058: Because this call is not awaited, execution of the current method continues before the call is completed
            // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/functional/discards?WT.mc_id=DT-MVP-5003978#a-standalone-discard
            _ = ForgetAwaited(task);
        }

        // Allocate the async/await state machine only when needed for performance reasons.
        // More info about the state machine: https://blogs.msdn.microsoft.com/seteplia/2017/11/30/dissecting-the-async-methods-in-c/?WT.mc_id=DT-MVP-5003978
        async static Task ForgetAwaited(Task task)
        {
            try
            {
                // No need to resume on the original SynchronizationContext, so use ConfigureAwait(false)
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Nothing to do here
            }
        }
    }
}
