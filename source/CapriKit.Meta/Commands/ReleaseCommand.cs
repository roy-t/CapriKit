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

        //if (!DotNetManager.Restore(solution))
        //{
        //    AnsiConsoleExt.ErrorMarkupLineInterpolated($"Package restore failed");
        //    return 20;
        //}
        //if (!DotNetManager.Format(solution))
        //{
        //    AnsiConsoleExt.ErrorMarkupLineInterpolated($"Code formatting failed");
        //    return 30;
        //}
        //if (!DotNetManager.Test(solution))
        //{
        //    AnsiConsoleExt.ErrorMarkupLineInterpolated($"Tests failed");
        //    return 40;
        //}

        // TODO: have to call this metnhod since BuildLogger has public elements that reference MSBuild, fix by encapsulating it
        // TODO: have to capture status of build somehow and stop if it fails
        MSBuildManager.InitializeMsBuild();
        AnsiConsole.Status().Start($"Initializing build of {solution}", progress =>
        {
            
            var logger = new BuildLogger(progress);
            progress.Spinner(Spinner.Known.Dots);
            MSBuildManager.BuildAndPackSolution(logger, solution);
        });
        

        //if ()
        //{
        //    AnsiConsoleExt.ErrorMarkupLineInterpolated($"Building and packing failed");
        //    return 50;
        //}

        if (release.DryRun)
        {
            AnsiConsoleExt.WarningMarkupLineInterpolated($"Dry run, skipping NuGet push");
        }
        else
        {
            var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
            var packagePath = Path.Combine(solutionPath, ".build", "pkg");

            if (DotNetManager.NuGetPush(packagePath, release.ApiKey ?? string.Empty))
            {
                AnsiConsoleExt.ErrorMarkupLineInterpolated($"Pushing to NuGet failed");
            }
        }

        return 0;
    }
}
