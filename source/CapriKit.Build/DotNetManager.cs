using System.Diagnostics;

namespace CapriKit.Build;

public static class DotNetManager
{
    /// <summary>
    /// Runs `dotnet restore` on all projects in the solution.
    /// </summary>    
    public static BuildTask Restore(StreamWriter logStream, string solutionPath)
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
    public static BuildTask Format(StreamWriter logStream, string solutionPath)
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
    public static BuildTask Test(StreamWriter logStream, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        var argumentList = new List<string>();
        argumentList.Add("test");
        argumentList.Add(solutionPath);
        argumentList.Add("--no-restore");
        AppendLogArguments(argumentList);

        return CreateTask(logStream, argumentList, solutionDirectory, $"dotnet test for {solutionPath}");
    }

    /// <summary>
    /// Runs `dotnet nuget push *.nupkg` in the directory.
    /// </summary>    
    public static BuildTask NuGetPush(StreamWriter logStream, string packageDirectory, string apiKey)
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

    private static BuildTask CreateTask(StreamWriter writer, IReadOnlyCollection<string> argumentList, string? workingDirectory, string message)
    {
        return new BuildTask(() =>
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
                    return new BuildTaskResult(true);
                }
                {
                    writer.WriteLine($"Failed {message}, exit code: {exitCode}");
                    return new BuildTaskResult(false, new Exception($"Process failed with exit code {exitCode}"));
                }
            }
            else
            {
                writer.WriteLine("Could not start process");
                return new BuildTaskResult(false, new Exception("Could not start process"));
            }
        });
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
