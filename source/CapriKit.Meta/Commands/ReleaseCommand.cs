using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
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

        var results = new List<TaskExecutionResult>();

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
            var taskList = new TaskList(context);
            taskList.AddTask("Restore", DotNetManager.Restore(logStreamWriter, solutionPath));
            taskList.AddTask("Format", DotNetManager.Format(logStreamWriter, solutionPath));
            taskList.AddTask("Test", DotNetManager.Test(logStreamWriter, solutionPath));
            taskList.AddTask("Build", MSBuildManager.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, WellKnownTargets.Build));
            taskList.AddTask("Pack", MSBuildManager.BuildSolution(logStreamWriter, solution, WellKnownConfigurations.Release, WellKnownTargets.Pack));

            if (release.DryRun)
            {
                taskList.AddTask("Publish", []);
            }
            else
            {
                taskList.AddTask("Publish", () => DotNetManager.NuGetPush(logStreamWriter, packagePath, release.ApiKey));
            }

            results.AddRange(taskList.Execute());
        });

        foreach (var result in results)
        {
            if (result.Exception != null)
            {
                AnsiConsoleExt.ErrorMarkupLineInterpolated($"Task {result.Description} failed");
                AnsiConsole.WriteException(result.Exception);
            }
        }

        return 0;
    }

    public override int Execute(CommandContext context, Settings release)
    {
        return ExecuteInternal(context, release);
    }
}
