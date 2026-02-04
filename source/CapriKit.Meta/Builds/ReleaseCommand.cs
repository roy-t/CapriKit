using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace CapriKit.Meta.Builds;

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
        var solutionPath = Config.SolutionPath;
        var pacakgeDirectory = Config.Outputs.PackageDirectory;
        var testResultsDirectory = Config.Outputs.TestDirectory;
        var testResultsFileName = Config.Outputs.TestResultsFileName;

        using var logger = BuildLogger.CreateBuildLogger();

        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Format", DotNetManager.Format(logger.Writer, solutionPath));
        taskList.AddTask("Build Test", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Test));
        taskList.AddTask("Test", DotNetManager.Test(logger.Writer, solutionPath, WellKnownConfigurations.Test, testResultsDirectory, testResultsFileName));
        taskList.AddTask("Build Release", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Release));
        taskList.AddTask("Pack", DotNetManager.Pack(logger.Writer, solutionPath, WellKnownConfigurations.Release));

        if (release.DryRun)
        {
            taskList.AddTask("Push", []);
        }
        else
        {
            taskList.AddTask("Push", () => DotNetManager.NuGetPush(logger.Writer, pacakgeDirectory, release.ApiKey));
        }

        var results = taskList.Execute(logger, cancellationToken);
        return TaskList.ExitCodeFromResult(results);
    }
}
