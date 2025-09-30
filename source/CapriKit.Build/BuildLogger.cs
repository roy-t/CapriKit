using CapriKit.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Spectre.Console;

namespace CapriKit.Meta.Commands;

public sealed class BuildLogger : ILogger
{
    private readonly StatusContext progress;

    public BuildLogger(StatusContext progress)
    {        
        this.progress = progress;
    }    


    public LoggerVerbosity Verbosity { get; set; }
    public string? Parameters { get; set; }

    public void Shutdown() { }

    public void Initialize(IEventSource eventSource)
    {
        if (eventSource != null)
        {
            eventSource.BuildStarted += BuildStartedHandler;
            eventSource.BuildFinished += BuildFinishedHandler;
            eventSource.ProjectStarted += InfoEventHandler;
            eventSource.ProjectFinished += InfoEventHandler;
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

    private void BuildStartedHandler(object sender, BuildStartedEventArgs e)
    {
        progress.Status(e.Message ?? string.Empty);
        progress.Spinner(Spinner.Known.Star);
        progress.SpinnerStyle(Style.Parse("green"));
    }

    private void BuildFinishedHandler(object sender, BuildFinishedEventArgs e)
    {
        progress.Status(e.Message ?? string.Empty);
    }

    private void InfoEventHandler(object sender, BuildEventArgs e)
    {
        InfoMarkupLineInterpolated($"{e.Message ?? ""}");
    }

    private void WarningEventHandler(object sender, BuildEventArgs e)
    {
        WarningMarkupLineInterpolated($"{e.Message ?? ""}");        
        progress.SpinnerStyle(Style.Parse("yellow"));
    }

    private void ErrorEventHandler(object sender, BuildEventArgs e)
    {
        ErrorMarkupLineInterpolated($"{e.Message ?? ""}");
        progress.SpinnerStyle(Style.Parse("red"));
    }

    // TODO: duplicated from Caprikit.Meta!
    public static void InfoMarkupLineInterpolated(FormattableString markup)
    {
        WriteInfo();
        AnsiConsole.MarkupLineInterpolated(markup);
    }

    public static void WarningMarkupLineInterpolated(FormattableString markup)
    {
        WriteWarning();
        AnsiConsole.MarkupLineInterpolated(markup);
    }

    public static void ErrorMarkupLineInterpolated(FormattableString markup)
    {
        WriteError();
        AnsiConsole.MarkupLineInterpolated(markup);
    }


    private static void WriteInfo()
    {
        const string INFO = "[INFO]";
        AnsiConsole.MarkupInterpolated($"[bold grey]{INFO}[/]: ");
    }

    private static void WriteWarning()
    {
        const string WARNING = "[WARN]";
        AnsiConsole.MarkupInterpolated($"[bold orangered1]{WARNING}[/]: ");
    }

    private static void WriteError()
    {
        const string ERROR = "[ERROR]";
        AnsiConsole.MarkupInterpolated($"[bold red]{ERROR}[/]: ");
    }
}
