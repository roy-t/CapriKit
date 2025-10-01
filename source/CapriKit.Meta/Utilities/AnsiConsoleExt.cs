using Spectre.Console;

namespace CapriKit.Meta.Utilities;

public static class AnsiConsoleExt
{

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

    public static void WriteBuildResult(int warnings, int errors, bool succeeded)
    {
        if (succeeded)
        {
            if (warnings > 0 || errors > 0)
            {
                AnsiConsoleExt.WarningMarkupLineInterpolated($"Build succeeded: {warnings} warning(s), {errors} error(s)");
            }
            else
            {
                AnsiConsoleExt.InfoMarkupLineInterpolated($"Build succeeded: {warnings} warning(s), {errors} error(s)");
            }
        }
        else
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Build failed: {warnings} warning(s), {errors} error(s)");
        }
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
