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

    public override int Execute(CommandContext context, Settings release, CancellationToken cancellationToken)
    {
        MSBuildManager.InitializeMsBuild();

        var (solutionPath, packagePath, testResultsDirectory, testResultsFileName, _) = BuildUtilities.GatherBuildInputs();
        using var logger = BuildUtilities.CreateBuildLogger();

        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Format", DotNetManager.Format(logger.Writer, solutionPath));
        taskList.AddTask("Build Test", MSBuildManager.BuildSolution(logger.Writer, solutionPath, WellKnownConfigurations.Test, WellKnownTargets.Build));
        taskList.AddTask("Test", DotNetManager.Test(logger.Writer, solutionPath, testResultsDirectory, testResultsFileName));
        taskList.AddTask("Build Release", MSBuildManager.BuildSolution(logger.Writer, solutionPath, WellKnownConfigurations.Release, WellKnownTargets.Build));
        taskList.AddTask("Pack", MSBuildManager.BuildSolution(logger.Writer, solutionPath, WellKnownConfigurations.Release, WellKnownTargets.Pack));

        if (release.DryRun)
        {
            taskList.AddTask("Publish", []);
        }
        else
        {
            taskList.AddTask("Publish", () => DotNetManager.NuGetPush(logger.Writer, packagePath, release.ApiKey));
        }

        var results = taskList.Execute(cancellationToken);        
        return TaskList.ExitCodeFromResult(results);
    }
}
