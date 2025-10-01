using CapriKit.Build;
using Spectre.Console;

namespace CapriKit.Meta.Utilities;

internal class SpectreProgressTracker(StatusContext context) : IProgressTracker
{
    public int Warnings { get; private set; }
    public int Errors { get; private set; }
    public bool Succeeded { get; private set; }

    public void HandleStarted(string message)
    {
        context.Status(message);
        context.Spinner = Spinner.Known.DotsCircle;
    }

    public void HandleTaskStarted(string message)
    {
        context.Status(message);
    }

    public void HandleTaskCompleted(string message, bool succeeded)
    {
        context.Status(message);
    }

    public void HandleCompleted(string message, bool succeeded)
    {
        Succeeded = succeeded;
        context.Status(message);
    }

    public void HandleInfoMessage(string message)
    {
        AnsiConsole.WriteLine(message);
    }

    public void HandleWarningMessage(string message)
    {
        Warnings++;
        AnsiConsole.WriteLine(message);
    }

    public void HandleErrorMessage(string message)
    {
        Errors++;
        AnsiConsole.WriteLine(message);
    }
}
