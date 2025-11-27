using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using static CapriKit.Build.MSBuildManager2;

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


    private static int ExecuteInternal(CommandContext _, Settings release)
    {
        MSBuildManager2.InitializeMsBuild();
        var solution = FileSearchUtilities.SearchFileUp("*.sln").FirstOrDefault();
        if (solution == null)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");
            return 10;
        }

        var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
        var packagePath = Path.Combine(solutionPath, ".build", "pkg");

        using var logStream = new MemoryStream();
        using var logStreamWriter = new StreamWriter(logStream);

        var result = new BuildTaskResult(true, null);
        AnsiConsole.Progress()
            .Columns([
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),                
                new OutcomeColumn(),
                new SpinnerColumn(),
                ])
            .Start(context =>
        {
            var buildTask = MSBuildManager2.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, [WellKnownTargets.Build]);
            var buildProgress = context.AddAggregateTask("Build", buildTask);

            var packTask = MSBuildManager2.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, [WellKnownTargets.Pack]);
            var packProgress = context.AddAggregateTask("Pack", packTask);
            
            context.RunAggregateTask(buildProgress, buildTask);
            context.RunAggregateTask(packProgress, packTask);

            // TODO: run each task one by one, stop on the first failure, print exception and provide link to logs
        });

        // TODO: this creates two files and never removes them?
        // Better to just create an IO library for this that uses rotating file logs
        var logFilePath = Path.GetTempFileName(); 
        logFilePath = Path.ChangeExtension(logFilePath, ".log");
        File.WriteAllBytes(logFilePath, logStream.ToArray());        
        AnsiConsole.MarkupLineInterpolated($"Logs stored in: [link]{logFilePath}[/]");
        return 0;
    }

    public override int Execute(CommandContext context, Settings release)
    {
        return ExecuteInternal(context, release);

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
