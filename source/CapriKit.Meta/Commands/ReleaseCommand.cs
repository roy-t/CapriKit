using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CapriKit.Meta.Commands;

internal sealed class ReleaseCommand : Command<ReleaseCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Performs all the release steps without actually pushing the package to NuGet")]
        [CommandOption("--dry-run")]
        public bool DryRun { get; init; }

        [Description("The api key required for uploading the package to NuGet")]
        [CommandOption("--key", true)]
        public string? ApiKey { get; init; }
    }

    public override int Execute(CommandContext context, Settings release)
    {
        var solution = FileSearchUtilities.SearchFileUp("*.sln").FirstOrDefault();
        if (solution == null)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");
            return 10;
        }

        if (!Restore(solution))
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Package restore failed");
            return 20;
        }

        if (!Format(solution))
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Code formatting failed");
            return 30;
        }
        if (!Test(solution))
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Tests failed");
            return 40;
        }

        if (!BuildAndPack(solution))
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Building and packing failed");
            return 50;
        }

        if (release.DryRun)
        {
            AnsiConsoleExt.WarningMarkupLineInterpolated($"Dry run, skipping NuGet push");
        }
        else
        {
            var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
            var packagePath = Path.Combine(solutionPath, ".build", "pkg");

            if (!Push(solution, packagePath, release.ApiKey ?? string.Empty))
            {
                AnsiConsoleExt.ErrorMarkupLineInterpolated($"Pushing to NuGet failed");
            }
        }

        return 0;
    }

    private bool Restore(string solution)
    {
        return RunTask(solution, $"Restoring packages of {solution}", tracker => DotNetManager.Restore(tracker, solution));
    }

    private bool Format(string solution)
    {
        return RunTask(solution, $"Formatting {solution}", tracker => DotNetManager.Format(tracker, solution));
    }

    private bool Test(string solution)
    {
        return RunTask(solution, $"Testing {solution}", tracker => DotNetManager.Test(tracker, solution));
    }

    private bool Push(string solution, string packagePath, string apiKey)
    {
        return RunTask(solution, $"Publishing {solution}", tracker => DotNetManager.NuGetPush(tracker, packagePath, apiKey));
    }

    private bool BuildAndPack(string solution)
    {
        return RunTask(solution, $"Building {solution}", tracker => MSBuildManager.BuildAndPackSolution(tracker, solution));
    }

    private static bool RunTask(string solution, string message, Action<IProgressTracker> task)
    {
        var succeeded = false;
        AnsiConsole.Status().Start(message, progress =>
        {
            var tracker = new SpectreProgressTracker(progress);
            progress.Spinner(Spinner.Known.Dots);
            task(tracker);

            AnsiConsoleExt.WriteBuildResult(tracker.Warnings, tracker.Errors, tracker.Succeeded);
            succeeded = tracker.Succeeded;
        });

        return succeeded;
    }
}
