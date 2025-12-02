using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using static CapriKit.Build.MSBuildManager;

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
        public string ApiKey { get; init; } = string.Empty;
    }


    private static int ExecuteInternal(CommandContext _, Settings release)
    {
        MSBuildManager.InitializeMsBuild();
        var solution = FileSearchUtilities.SearchFileUp("*.sln").FirstOrDefault();
        if (solution == null)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");
            return 10;
        }

        var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
        var packagePath = Path.Combine(solutionPath, ".build", "pkg");

        var logFile = FileRotator.CreateFile(Directory.GetCurrentDirectory(), "release", ".log", 10);
        using var logStream = logFile.OpenWrite();
        using var logStreamWriter = new StreamWriter(logStream);

        AnsiConsole.MarkupLineInterpolated($"Logging to: [link={logFile.FullName}]{logFile.Name}[/]");

        var result = new BuildTaskResult(true);
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
            // TODO: its a bit messy have to manually do all this administration here.
            // Can we make a fire and forget mechanism?
            var restoreTask = DotNetManager.Restore(logStreamWriter, solutionPath);
            var restoreProgress = context.AddBuildTask("Restore", restoreTask);

            var formatTask = DotNetManager.Format(logStreamWriter, solutionPath);
            var formatProgress = context.AddBuildTask("Format", formatTask);

            var testTask = DotNetManager.Test(logStreamWriter, solutionPath);
            var testProgress = context.AddBuildTask("Test", testTask);

            var buildTask = MSBuildManager.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, WellKnownTargets.Build);
            var buildProgress = context.AddBuildTask("Build", buildTask);

            var packTask = MSBuildManager.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, WellKnownTargets.Pack);
            var packProgress = context.AddBuildTask("Pack", packTask);

            ProgressTask? publishProgress = null;
            BuildTask? publishTask = null;

            if (release.DryRun)
            {
                publishProgress = context.AddTask("Publish");
                publishProgress.State.Update<OutcomeColumn.Outcome>(OutcomeColumn.OutcomeKey, _ => OutcomeColumn.Outcome.Skipped);
                publishProgress.StopTask();
            }
            else
            {
                publishTask = DotNetManager.NuGetPush(logStreamWriter, packagePath, release.ApiKey);
                publishProgress = context.AddBuildTask("Publish", publishTask);
            }

            context.RunBuildTask(restoreProgress, restoreTask);
            context.RunBuildTask(formatProgress, formatTask);
            context.RunBuildTask(testProgress, testTask);
            context.RunBuildTask(buildProgress, buildTask);
            context.RunBuildTask(packProgress, packTask);

            if (publishTask != null)
            {
                context.RunBuildTask(publishProgress, publishTask);
            }
        });


        return 0;
    }

    public override int Execute(CommandContext context, Settings release)
    {
        return ExecuteInternal(context, release);
    }
}
