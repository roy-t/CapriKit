using CapriKit.Meta.Commands;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Microsoft.Build.Locator;

namespace CapriKit.Build;

public class MSBuildManager
{
    // This method has to be called before any method that references any types from Microsoft.Build
    // https://learn.microsoft.com/en-us/visualstudio/msbuild/find-and-use-msbuild-versions?view=vs-2019#register-instance-before-calling-msbuild
    private static void InitializeMsBuild()
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
    public static void BuildProject(IProgressTracker tracker, string projectPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildProjectInternal(tracker, projectPath, configuration, "build");
    }

    /// <summary>
    /// Builds all MSBuild projects referenced in the given Visual Studio Solution file.
    /// </summary>    
    /// <returns>True if the all builds succeeded, false otherwise</returns>
    public static void BuildSolution(IProgressTracker tracker, string solutionPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildSolutionInternal(tracker, solutionPath, configuration, "build");
    }

    /// <summary>
    /// Packages all MSBuild projects referenced in the given Visual Studio Solution file.
    /// </summary>    
    /// <returns>True if the all builds succeeded, false otherwise</returns>
    public static void BuildAndPackSolution(IProgressTracker tracker, string solutionPath, string configuration = "Release")
    {
        InitializeMsBuild();
        BuildSolutionInternal(tracker, solutionPath, configuration, "build", "pack");
    }

    private static void BuildProjectInternal(IProgressTracker tracker, string projectUrl, string configuration, params string[] targetsToBuild)
    {
        var globalProperties = new Dictionary<string, string?>
        {
            { "Configuration", configuration }
        };

        var buildParameters = new BuildParameters
        {
            Loggers = [new MSBuildLogger(tracker)]
        };

        var buildRequest = new BuildRequestData(projectUrl, globalProperties, null, targetsToBuild, null);
        var result = BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
    }

    private static void BuildSolutionInternal(IProgressTracker tracker, string solutionPath, string configuration, params string[] targetsToBuild)
    {
        SolutionFile solution = OpenSolution(solutionPath);
        var projects = solution.ProjectsInOrder
            .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat);
        
        var solutionDirectory = Path.GetDirectoryName(solutionPath)!;
        foreach (var project in projects)
        {
            var projectPath = Path.Combine(solutionDirectory, project.RelativePath);
            BuildProjectInternal(tracker, projectPath, configuration, targetsToBuild);
        }
    }

    public static int GetProjectCount(string solutionPath)
    {
        SolutionFile solution = OpenSolution(solutionPath);
        var projects = solution.ProjectsInOrder
            .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat);

        return projects.Count();
    }

    private static SolutionFile OpenSolution(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        if (!File.Exists(solutionPath) || solutionDirectory == null)
        {
            throw new Exception($"Invalid solution path: {solutionPath}");
        }

        var solution = SolutionFile.Parse(solutionPath);
        return solution;
    }
}
