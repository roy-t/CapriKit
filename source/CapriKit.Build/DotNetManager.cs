using System.Diagnostics;

namespace CapriKit.Build;

public static class DotNetManager
{
    /// <summary>
    /// Runs `dotnet build --no-restore` on a solution or project. Defaults to the build targets if none specified    
    /// </summary>    
    public static Action Build(StreamWriter logStream, string path, string configuration, params string[] targets)
    {
        var workingDirectory = Path.GetDirectoryName(path);

        var argumentList = new List<string>();
        argumentList.Add("build");
        argumentList.Add(path);
        argumentList.Add("--no-restore");
        argumentList.Add("--configuration");
        argumentList.Add(configuration);
        if (targets.Length > 0)
        {
            argumentList.Add($"-t:{string.Join(',', targets)}");
        }
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, workingDirectory, $"dotnet build for {path}");

    }

    /// <summary>
    /// Runs `dotnet pack --no-restore` on a solution or project. Defaults to the build targets if none specified    
    /// </summary>    
    public static Action Pack(StreamWriter logStream, string path, string configuration)
    {
        var workingDirectory = Path.GetDirectoryName(path);

        var argumentList = new List<string>();
        argumentList.Add("pack");
        argumentList.Add(path);
        argumentList.Add("--no-restore");
        argumentList.Add("--no-build");
        argumentList.Add("--include-symbols");
        argumentList.Add("--include-source");
        argumentList.Add("--configuration");
        argumentList.Add(configuration);
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, workingDirectory, $"dotnet build for {path}");

    }

    /// <summary>
    /// Runs `dotnet run` on one projects in the solution.
    /// Assumes the project has already been built
    /// </summary>    
    public static Action Run(StreamWriter logStream, string solutionPath, string projectPath, string configuration, params string[] args)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        var argumentList = new List<string>();
        argumentList.Add("run");
        argumentList.Add("--project");
        argumentList.Add(projectPath);
        argumentList.Add("--no-restore");
        argumentList.Add("--no-build");
        argumentList.Add("--configuration");
        argumentList.Add(configuration);
        AppendLogArguments(argumentList);
        if (args.Length > 0)
        {
            argumentList.Add("--");
            argumentList.AddRange(args);
        }

        return CreateTask(logStream, argumentList, solutionDirectory, $"dotnet run for {projectPath}");
    }


    /// <summary>
    /// Runs `dotnet restore` on all projects in the solution.
    /// </summary>    
    public static Action Restore(StreamWriter logStream, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        var argumentList = new List<string>();
        argumentList.Add("restore");
        argumentList.Add(solutionPath);
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, solutionDirectory, $"dotnet restore for {solutionPath}");
    }


    /// <summary>
    /// Runs `dotnet format --no-restore` on all projects in the solution.
    /// </summary>    
    public static Action Format(StreamWriter logStream, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        var argumentList = new List<string>();
        argumentList.Add("format");
        argumentList.Add(solutionPath);
        argumentList.Add("--no-restore");
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, solutionDirectory, $"dotnet format for {solutionPath}");
    }

    /// <summary>
    /// Runs `dotnet test --no-restore` on all projects in the solution.
    /// </summary>    
    public static Action Test(StreamWriter logStream, string solutionPath, string configuration, string testReportDirectory, string testReportFileName)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        var argumentList = new List<string>();
        argumentList.Add("test");
        argumentList.Add("--solution");
        argumentList.Add(solutionPath);
        argumentList.Add("--no-build");
        argumentList.Add("--no-restore");
        argumentList.Add("--configuration");
        argumentList.Add(configuration);
        argumentList.Add("--results-directory");
        argumentList.Add(testReportDirectory);
        argumentList.Add("--report-trx");
        argumentList.Add("--report-trx-filename");
        argumentList.Add(testReportFileName);
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, solutionDirectory, $"dotnet test for {solutionPath}");
    }

    /// <summary>
    /// Runs `dotnet nuget push *.nupkg` in the directory.
    /// </summary>    
    public static Action NuGetPush(StreamWriter logStream, string packageDirectory, string apiKey)
    {
        var argumentList = new List<string>();

        // dotnet nuget .. does not support -v or --verbosity        
        argumentList.Add("nuget");
        argumentList.Add("push");
        argumentList.Add("*.nupkg");
        argumentList.Add("--source");
        argumentList.Add("https://api.nuget.org/v3/index.json");
        argumentList.Add("--skip-duplicate");
        argumentList.Add("--api-key");
        argumentList.Add(apiKey);


        return CreateTask(logStream, argumentList, packageDirectory, $"nuget push for {packageDirectory}");
    }

    private static void AppendLogArguments(ICollection<string> argumentList)
    {
        argumentList.Add("-v");
        argumentList.Add("normal");
    }

    private static Action CreateTask(StreamWriter writer, IReadOnlyCollection<string> argumentList, string? workingDirectory, string message)
    {
        return () =>
        {
            writer.WriteLine($"Started {message}");
            using var process = BootstrapDotNetProcess(writer, workingDirectory);
            foreach (var argument in argumentList)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (process.Start())
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                var exitCode = process.ExitCode;
                if (exitCode == 0)
                {
                    writer.WriteLine($"Completed {message}.");
                    return;
                }
                {
                    writer.WriteLine($"Failed {message}, exit code: {exitCode}");
                    throw new Exception($"Process failed with exit code {exitCode}");
                }
            }
            else
            {
                writer.WriteLine("Could not start process");
                throw new Exception($"Could not start process");
            }
        };
    }

    private static Process BootstrapDotNetProcess(StreamWriter logStream, string? workingDirectory)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (_, e) => logStream.WriteLine(e.Data);
        process.ErrorDataReceived += (_, e) => logStream.WriteLine(e.Data);

        return process;
    }
}
