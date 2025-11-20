namespace CapriKit.Build;

public interface IProgressTracker
{
    void HandleStarted(string message);
    void HandleCompleted(string message, bool succeeded);

    void HandleTaskStarted(string message);
    void HandleTaskCompleted(string message, bool succeeded);

    void HandleInfoMessage(string message);
    void HandleWarningMessage(string message);
    void HandleErrorMessage(string message);
}

public interface ISimpleProgressTracker
{
    void OnProgress(int progress);
    void OnCompleted(string message);
    void OnFailed(string message);
}
