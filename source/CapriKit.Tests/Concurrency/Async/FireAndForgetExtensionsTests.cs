using CapriKit.Concurrency.Async;

namespace CapriKit.Tests.Concurrency.Async;

internal class FireAndForgetExtensionsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public async Task FireAndForget()
    {
        var completed = new TaskCompletionSource();
        var work = new TaskCompletionSource();

        work.Task.FireAndForget(ex => { }, () => completed.SetResult());
        work.SetResult();

        await completed.Task.WaitAsync(Timeout);
    }

    [Test]
    public async Task FireAndForget_AlreadyCompletedTask()
    {
        var completed = new TaskCompletionSource();

        Task.CompletedTask.FireAndForget(ex => { }, () => completed.SetResult());

        await completed.Task.WaitAsync(Timeout);
    }

    [Test]
    public async Task FireAndForget_CancelledTask()
    {
        Exception? observed = null;
        var completed = new TaskCompletionSource();

        var cancelled = Task.FromCanceled(new CancellationToken(canceled: true));
        cancelled.FireAndForget(ex => observed = ex, () => completed.SetResult());

        await completed.Task.WaitAsync(Timeout);
        await Assert.That(observed).IsNull();
    }

    [Test]
    public async Task FireAndForget_FaultedTask()
    {
        Exception? observed = null;
        var completed = new TaskCompletionSource();

        var faulted = Task.FromException(new InvalidOperationException());
        faulted.FireAndForget(ex => observed = ex, () => completed.SetResult());

        await completed.Task.WaitAsync(Timeout);
        await Assert.That(observed).IsTypeOf<FaFTaskException>();
        await Assert.That(observed!.InnerException).IsTypeOf<InvalidOperationException>();
    }
}
