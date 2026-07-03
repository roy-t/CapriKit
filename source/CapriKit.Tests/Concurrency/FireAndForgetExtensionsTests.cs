using CapriKit.Concurrency.Async;

namespace CapriKit.Tests.Concurrency;

internal class FireAndForgetExtensionsTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Test]
    public async Task OnCompleted_Is_Invoked_After_A_Successful_Task()
    {
        var completed = new TaskCompletionSource();
        var work = new TaskCompletionSource();

        work.Task.FireAndForget(ex => { }, () => completed.SetResult());
        work.SetResult();

        await completed.Task.WaitAsync(Timeout);
    }

    [Test]
    public async Task OnCompleted_Is_Invoked_For_An_Already_Completed_Task()
    {
        var completed = new TaskCompletionSource();

        Task.CompletedTask.FireAndForget(ex => { }, () => completed.SetResult());

        await completed.Task.WaitAsync(Timeout);
    }

    [Test]
    public async Task OnCompleted_Is_Invoked_For_A_Cancelled_Task_Without_Reporting_An_Exception()
    {
        Exception? observed = null;
        var completed = new TaskCompletionSource();

        var cancelled = Task.FromCanceled(new CancellationToken(canceled: true));
        cancelled.FireAndForget(ex => observed = ex, () => completed.SetResult());

        await completed.Task.WaitAsync(Timeout);
        await Assert.That(observed).IsNull();
    }

    [Test]
    public async Task OnException_Wraps_The_Failure_And_OnCompleted_Is_Still_Invoked()
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
