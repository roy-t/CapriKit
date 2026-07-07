using CapriKit.Concurrency.Primitives;
using System.Runtime.ExceptionServices;

namespace CapriKit.Concurrency.Async;

/// <summary>
/// Schedules a batch of jobs that are run asynchronously and in parallel
/// </summary>
public static class BackgroundWorker
{
    public static Drain<T> Create<T>(params IReadOnlyList<Job<T>> jobs)
    {
        var cancellation = new CancellationTokenSource();
        var channel = new LightweightChannel<JobResult<T>>();
        var drain = new Drain<T>(cancellation, channel);

        Parallel.ForEachAsync(jobs, cancellation.Token, async (job, token) =>
        {
            // Note: we are not passing the cancellation token to the writer because we need the writes
            // of a potentially disposable object to always succeed, otherwise we cannot drain it to
            // clean it up.
            try
            {
                drain.OnJobStarted();
                var result = await job.Callback(token);
                channel.Write(JobResult<T>.Success(job.Id, result));
            }
            catch (OperationCanceledException)
            {
                // A cancelled job is not a failure and has nothing to publish
            }
            catch (Exception ex)
            {
                var info = ExceptionDispatchInfo.Capture(ex);
                channel.Write(JobResult<T>.Failure(job.Id, info));
            }
            finally
            {
                drain.OnJobCompleted();
            }
        }).FireAndForget(exception => channel.Write(exception));

        return drain;
    }
}
