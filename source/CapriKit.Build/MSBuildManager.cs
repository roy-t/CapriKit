using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;

namespace CapriKit.Build;

public class MSBuildManager
{
    private const int MSBUILD_SUCCESS = 0;
    private const int MSBUILD_FAILURE = 1;

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
    /// Builds the given MSBuild project file. 
    /// </summary>    
    /// <returns>True if the build succeeded, false otherwise</returns>
    public static void BuildProject(ILogger logger, string projectPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildProjectInternal(logger, projectPath, configuration, "build");
    }

    /// <summary>
    /// Builds all MSBuild projects referenced in the given Visual Studio Solution file.
    /// </summary>    
    /// <returns>True if the all builds succeeded, false otherwise</returns>
    public static void BuildSolution(ILogger logger, string solutionPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildSolutionInternal(logger, solutionPath, configuration, "build");
    }

    /// <summary>
    /// Packages all MSBuild projects referenced in the given Visual Studio Solution file.
    /// </summary>    
    /// <returns>True if the all builds succeeded, false otherwise</returns>
    public static void BuildAndPackSolution(ILogger logger, string solutionPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildSolutionInternal(logger, solutionPath, configuration, "build", "pack");
    }

    private static void BuildProjectInternal(ILogger logger, string projectUrl, string configuration, params string[] targetsToBuild)
    {
        var globalProperties = new Dictionary<string, string?>
        {
            { "Configuration", configuration }
        };

        var buildParameters = new BuildParameters
        {
            Loggers = [logger]
        };

        var buildRequest = new BuildRequestData(projectUrl, globalProperties, null, targetsToBuild, null);
        var result = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
    }

    private static void BuildSolutionInternal(ILogger logger, string solutionPath, string configuration, params string[] targetsToBuild)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (!File.Exists(solutionPath) || solutionDirectory == null)
        {
            throw new Exception($"Invalid solution path: {solutionPath}");
        }

        var solution = SolutionFile.Parse(solutionPath);
        var projects = solution.ProjectsInOrder
            .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat);

        foreach (var project in projects)
        {
            var projectPath = Path.Combine(solutionDirectory, project.RelativePath);
            BuildProjectInternal(logger, projectPath, configuration, targetsToBuild);
        }
    }
}
