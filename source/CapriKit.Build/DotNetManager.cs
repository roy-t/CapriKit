using System.Diagnostics;

namespace CapriKit.Build;

public static class DotNetManager
{
    /// <summary>
    /// Runs `dotnet restore` on all projects in the solution.
    /// </summary>    
    public static void Restore(IProgressTracker tracker, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        using var process = BootstrapProcess(tracker, "dotnet", solutionDirectory);

        process.StartInfo.ArgumentList.Add("restore");
        process.StartInfo.ArgumentList.Add(solutionPath);

        AppendLoggerArguments(process);
        RunProcess(tracker, $"dotnet restore for {solutionPath}", process);
    }


    /// <summary>
    /// Runs `dotnet format --no-restore` on all projects in the solution.
    /// </summary>    
    public static void Format(IProgressTracker tracker, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        using var process = BootstrapProcess(tracker, "dotnet", solutionDirectory);

        process.StartInfo.ArgumentList.Add("format");
        process.StartInfo.ArgumentList.Add(solutionPath);
        process.StartInfo.ArgumentList.Add("--no-restore");

        AppendLoggerArguments(process);
        RunProcess(tracker, $"dotnet format for {solutionPath}", process);
    }

    /// <summary>
    /// Runs `dotnet test --no-restore` on all projects in the solution.
    /// </summary>    
    public static void Test(IProgressTracker tracker, string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        using var process = BootstrapProcess(tracker, "dotnet", solutionDirectory);

        process.StartInfo.ArgumentList.Add("test");
        process.StartInfo.ArgumentList.Add("--no-restore");

        AppendLoggerArguments(process);
        RunProcess(tracker, $"dotnet test for {solutionPath}", process);
    }

    /// <summary>
    /// Runs `dotnet nuget push *.nupkg` in the directory.
    /// </summary>    
    public static void NuGetPush(IProgressTracker tracker, string packageDirectory, string apiKey)
    {
        using var process = BootstrapProcess(tracker, "dotnet", packageDirectory);

        process.StartInfo.ArgumentList.Add("nuget");
        process.StartInfo.ArgumentList.Add("push");
        process.StartInfo.ArgumentList.Add("*.nupkg");
        process.StartInfo.ArgumentList.Add("--source");
        process.StartInfo.ArgumentList.Add("https://api.nuget.org/v3/index.json");
        process.StartInfo.ArgumentList.Add("--skip-duplicate");
        process.StartInfo.ArgumentList.Add("--api-key");
        process.StartInfo.ArgumentList.Add(apiKey);

        // dotnet nuget .. does not support -v or --verbosity
        RunProcess(tracker, $"nuget push for {packageDirectory}", process);
    }

    private static Process BootstrapProcess(IProgressTracker tracker, string executable, string? workingDirectory)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += (_, e) => tracker.HandleInfoMessage(e.Data ?? string.Empty);
        process.ErrorDataReceived += (_, e) => tracker.HandleErrorMessage(e.Data ?? string.Empty);

        return process;
    }

    private static void AppendLoggerArguments(Process process)
    {
        process.StartInfo.ArgumentList.Add("-v");
        process.StartInfo.ArgumentList.Add("normal");
    }

    private static void RunProcess(IProgressTracker tracker, string message, Process process)
    {
        tracker.HandleStarted($"Started {message}");
        if (process.Start())
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var exitCode = process.ExitCode;

            tracker.HandleCompleted($"Completed {message}", exitCode == 0);
        }
        else
        {
            tracker.HandleCompleted("Could not start process", false);
        }
    }
}
