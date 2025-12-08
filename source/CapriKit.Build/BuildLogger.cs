using Microsoft.Build.Framework;

namespace CapriKit.Build;

internal sealed class MSBuildLogger(IProgressTracker tracker) : ILogger
{
    public LoggerVerbosity Verbosity { get; set; }
    public string? Parameters { get; set; }

    public void Initialize(IEventSource eventSource)
    {
        if (eventSource != null)
        {
            eventSource.BuildStarted += BuildStartedHandler;
            eventSource.BuildFinished += BuildFinishedHandler;
            eventSource.ProjectStarted += ProjectStartedHandler;
            eventSource.ProjectFinished += ProjectFinishedHandler;
            eventSource.TargetStarted += InfoEventHandler;
            eventSource.TargetFinished += InfoEventHandler;
            eventSource.TaskStarted += InfoEventHandler;
            eventSource.TaskFinished += InfoEventHandler;
            eventSource.ErrorRaised += ErrorEventHandler;
            eventSource.WarningRaised += WarningEventHandler;
            eventSource.MessageRaised += InfoEventHandler;
            eventSource.CustomEventRaised += InfoEventHandler;
            eventSource.StatusEventRaised += InfoEventHandler;
        }
    }

    public void Shutdown() { }

    private void BuildStartedHandler(object sender, BuildStartedEventArgs e)
    {
        tracker.HandleStarted(e.Message ?? string.Empty);
    }

    private void BuildFinishedHandler(object sender, BuildFinishedEventArgs e)
    {
        tracker.HandleCompleted(e.Message ?? string.Empty, e.Succeeded);
    }

    private void ProjectStartedHandler(object sender, ProjectStartedEventArgs e)
    {
        tracker.HandleTaskStarted(e.Message);
    }

    private void ProjectFinishedHandler(object sender, ProjectFinishedEventArgs e)
    {
        tracker.HandleTaskCompleted(e.Message, e.Succeeded);
    }

    private void InfoEventHandler(object sender, BuildEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message))
        {
            tracker.HandleInfoMessage(e.Message ?? string.Empty);
        }
    }

    private void WarningEventHandler(object sender, BuildEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message))
        {
            tracker.HandleWarningMessage(e.Message ?? string.Empty);
        }
    }

    private void ErrorEventHandler(object sender, BuildEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Message))
        {
            tracker.HandleErrorMessage(e.Message ?? string.Empty);
        }
    }
}
