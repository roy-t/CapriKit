using System.Diagnostics;

namespace CapriKit.Build;

public static class DotNetManager
{
    /// <summary>
    /// Runs `dotnet format --no-restore` on all projects in the solution.
    /// </summary>    
    public static bool Format(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        using var process = BootstrapProcess("dotnet", solutionDirectory);        
        process.StartInfo.ArgumentList.Add("format");
        process.StartInfo.ArgumentList.Add(solutionPath);        
        process.StartInfo.ArgumentList.Add("--no-restore");

        return RunProcess(process);        
    }

    /// <summary>
    /// Runs `dotnet test --no-restore` on all projects in the solution.
    /// </summary>    
    public static bool Test(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(solutionPath);
        using var process = BootstrapProcess("dotnet", solutionDirectory);
        process.StartInfo.ArgumentList.Add("test");
        process.StartInfo.ArgumentList.Add("--no-restore");

        return RunProcess(process);
    }

    private static void Process_ErrorDataReceived(object _, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data);
    }

    private static void Process_OutputDataReceived(object _, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data);
    }

    private static Process BootstrapProcess(string executable, string? workingDirectory)
    {
        var process = new Process();

        process.StartInfo.FileName = "dotnet";
        process.StartInfo.WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;

        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_ErrorDataReceived;        

        return process;
    }

    private static bool RunProcess(Process process)
    {
        if (!process.StartInfo.ArgumentList.Contains("-v") && !process.StartInfo.ArgumentList.Contains("-verbosity"))
        {
            process.StartInfo.ArgumentList.Add("-v");
            process.StartInfo.ArgumentList.Add("normal");
        }
        
        if (process.Start())
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var exitCode = process.ExitCode;
            return exitCode == 0;
        }
        else
        {
            throw new Exception("Could not start process");
        }
    }
}
