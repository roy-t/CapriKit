using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;

namespace CapriKit.Build;

public static class MSBuildManager
{
    // This method has to be called before any method that references any types from Microsoft.Build
    // https://learn.microsoft.com/en-us/visualstudio/msbuild/find-and-use-msbuild-versions?view=vs-2019#register-instance-before-calling-msbuild
    public static void InitializeMsBuild()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }

    /// <summary>
    /// Builds all MSBuild projects referenced in the given Visual Studio Solution file.
    /// </summary>    
    public static IReadOnlyList<Action> BuildSolution(StreamWriter logStream, string solutionPath, string configuration, params string[] targetsToBuild)
    {
        var solution = LoadSolutionFile(solutionPath);
        return [.. solution.ProjectsInOrder
            .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
            .Select(p => BuildProject(logStream, p.AbsolutePath, configuration, targetsToBuild))];
    }

    /// <summary>
    /// Builds the given MSBuild project file. 
    /// </summary>    
    public static Action BuildProject(StreamWriter logStream, string projectPath, string configuration, params string[] targetsToBuild)
    {
        var globalProperties = new Dictionary<string, string?>
        {
            { "Configuration", configuration }
        };

        var buildParameters = new BuildParameters
        {
            Loggers = [new MSBuildStreamLogger(logStream)]
        };

        var buildRequest = new BuildRequestData(projectPath, globalProperties, null, targetsToBuild, null);

        return () =>
        {
            var result = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
            if (result.OverallResult != BuildResultCode.Success)
            {
                throw result.Exception ?? new Exception("Build failed");
            }
        };
    }

    private static SolutionFile LoadSolutionFile(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (!File.Exists(solutionPath) || solutionDirectory == null)
        {
            throw new FileNotFoundException("Could not find solution", solutionPath);
        }

        var solution = SolutionFile.Parse(solutionPath);
        return solution;
    }

    public static class WellKnownTargets
    {
        public const string Build = "build";
        public const string Pack = "pack";
    }

    public static class WellKnownConfigurations
    {
        public const string Debug = "Debug";
        public const string Release = "Release";
    }
}
