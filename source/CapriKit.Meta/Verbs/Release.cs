using CapriKit.Build;
using CapriKit.CommandLine;

namespace CapriKit.Meta.Verbs;

/// <summary>
/// Runs formatting, test, build and pack steps before pushing to NuGet
/// </summary>
[Verb("release")]
public partial class Release
{
    /// <summary>
    /// Performs all the release steps without actually pushing the packages to NuGet
    /// </summary>
    [Flag("--dry-run")]
    public partial bool DryRun { get; }

    /// <summary>
    /// The api-key required for uploading the packages to NuGet.org
    /// </summary>
    [Flag("--api-key")]
    public partial string ApiKey { get; }

    public static void Execute(params string[] args)
    {
        var release = Parse(args);

        var solution = Utilities.SearchFileUp("*.sln").FirstOrDefault();
        if (solution != null)
        {
            if (!DotNetManager.Restore(solution))
            {
                Console.WriteLine("Error: restore failed");
                return;
            }
            if (!DotNetManager.Format(solution))
            {
                Console.WriteLine("Error: format failed");
            }
            if (!DotNetManager.Test(solution))
            {
                Console.WriteLine("Error: test failed");
            }
            if (!MSBuildManager.BuildAndPackSolution(solution))
            {
                Console.WriteLine("Error: build or pack failed");
            }

            if (release.DryRun)
            {
                Console.WriteLine("Dry run, skipping nuget push");
            }
            else
            {
                if (!release.HasApiKey)
                {
                    Console.WriteLine("Error: missing --api-key argument");
                }
                else
                {
                    var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
                    var packagePath = Path.Combine(solutionPath, ".build", "pkg");

                    if (DotNetManager.NuGetPush(packagePath, release.ApiKey))
                    {
                        Console.WriteLine("Error: nuget push failed");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");
        }
    }
}
