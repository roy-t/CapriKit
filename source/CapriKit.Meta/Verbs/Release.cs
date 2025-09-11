using CapriKit.Build;
using CapriKit.CommandLine;

namespace CapriKit.Meta.Verbs;

/// <summary>
/// Formats, builds, tests and packs the application, then pushes the packages to NuGet.org
/// </summary>
[Verb("release")]
public partial class Release
{
    /// <summary>
    /// Performs all the release steps without actually pushing the packages to NuGet
    /// </summary>
    [Flag("--dry-run")]
    public partial bool DryRun { get; }

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
                var solutionPath = Path.GetDirectoryName(solution) ?? Environment.CurrentDirectory;
                var packagePath = Path.Combine(solutionPath, ".build", "pkg");

                if (DotNetManager.NuGetPush(packagePath, "abc"))
                {
                    Console.WriteLine("Error: nuget push failed");
                }
            }
        }
        else
        {
            Console.WriteLine($"Could not find *.sln file in {Environment.CurrentDirectory} or parent directories");
        }
    }
}
