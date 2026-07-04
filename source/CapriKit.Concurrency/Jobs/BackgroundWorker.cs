using CapriKit.Concurrency.Async;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace CapriKit.Concurrency.Jobs;

public sealed class Drain<T> : IDisposable
{
    private readonly CancellationTokenSource Cancellation;
    private readonly Channel<CompletedJob<T>> Channel;

    public Drain(CancellationTokenSource cancellation, Channel<CompletedJob<T>> channel)
    {
        Cancellation = cancellation;
        Channel = channel;
    }

    public void Dispose()
    {

    }
}

public static class BackgroundWorker
{
    public static Drain<T> Create<T>(params IReadOnlyList<Job<T>> jobs)
    {
        var cancellation = new CancellationTokenSource();

        var channel = Channel.CreateUnbounded<CompletedJob<T>>(new()
        {
            SingleReader = true,
            SingleWriter = false,
        });

        Parallel.ForEachAsync(jobs, cancellation.Token, async (job, token) =>
        {
            // Note: we are not passing the cancellation token to the writer because we need the writes
            // of a potentially disposable object to always succeed, otherwise we cannot drain it to
            // clean it up.
            try
            {
                var result = await job.Callback(token);
                await channel.Writer.WriteAsync(CompletedJob<T>.Success(job.Id, result), CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                // A cancelled job is not a failure and has nothing to publish
            }
            catch (Exception ex)
            {
                var info = ExceptionDispatchInfo.Capture(ex);
                await channel.Writer.WriteAsync(CompletedJob<T>.Failure(job.Id, info), CancellationToken.None);
            }
        }).FireAndForget(
            ex => Trace.TraceError($"{nameof(BackgroundWorker)} failed: {ex}"),
            () => channel.Writer.Complete());

        return new Drain<T>(cancellation, channel);
    }
}
