using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;

namespace CapriKit.Meta.Commands;

// TODO: Ensure that the test filters can be passed on so that you can run a single test or single suite

internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        var (solutionPath, _, testResultsDirectory, _, benchmarkResultsDirectory) = BuildUtilities.GatherBuildInputs();
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var projectPath = Path.Combine(solutionDirectory, @"source\CapriKit.Benchmarks\CapriKit.Benchmarks.csproj");

        using var logger = BuildUtilities.CreateBuildLogger();
        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Build Release", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Release));
        taskList.AddTask("Benchmark", DotNetManager.Run(logger.Writer, solutionPath, projectPath, WellKnownConfigurations.Release, testResultsDirectory));

        var results = taskList.Execute(cancellationToken);
        if (results.Any(t => t.Exception != null))
        {
            return 1;
        }

        // Find each json file in benchmarkResultsDirectory that is newer than start time

        var directory = new DirectoryInfo(benchmarkResultsDirectory);
        if (!directory.Exists)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Benchmark report directory does not exist: {benchmarkResultsDirectory}");
            return 1;
        }

        var files = new List<FileInfo>();
        foreach (var file in directory.EnumerateFiles("*.json"))
        {
            if (file.LastWriteTime >= startTime)
            {
                files.Add(file);
            }
        }

        if (files.Any())
        {
            AnsiConsoleExt.InfoMarkupLineInterpolated($"Found {files.Count} benchmark reports in: {benchmarkResultsDirectory}");
        }
        else
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"No up-to-date benchmark reports found in: {benchmarkResultsDirectory}");
            return 1;
        }

        foreach (var file in files)
        {
            using var stream = file.OpenRead();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var benchmarkResults = JsonSerializer.Deserialize<BenchmarkResults>(json)!;
            
            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Test");
            table.AddColumn("Mean");
            table.AddColumn("Margin of Error"); // 99.9% sure the real mean is the estimated margin plus or minus this error
            table.AddColumn("StdDev");

            foreach (var result in benchmarkResults.Benchmarks)
            {
                // TODO: use the new math/statistics stuff to ensure we always compute the SD and Mean and .. in the same way
                // I've checked and for runs they match the outcomes from BenchmarkDotNet
                var mean2 = Mathematics.Statistics.Mean(result.Statistics.OriginalValues);                
                var deviation2 = Mathematics.Statistics.SampleStandardDeviation(mean2, result.Statistics.OriginalValues);
                var error2 = Mathematics.Statistics.StandardError(deviation2, result.Statistics.OriginalValues.Length);

                var name = result.FullName;
                var test = result.MethodTitle;
                var mean = $"{result.Statistics.Mean:F3} ns";
                var error = $"{result.Statistics.StandardError:F4} ns";
                var deviation = $"{result.Statistics.StandardDeviation:F4} ns";
                table.AddRow(name, test, mean, error, deviation);
            }

            AnsiConsole.Write(table);
        }

        // TODO: compare benchmark results with latest benchmark results and ask the user to 'promote' them if they are significantly different
        // if we want to do this correctly this means doing a Welch two-sample t-test
        // for example: https://github.com/accord-net/framework/blob/development/Sources/Accord.Statistics/Testing/TwoSample/TwoSampleTTest.cs#L195
        // note variance = (standard deviation)^2

        return TaskList.ExitCodeFromResult(results);
    }

    public sealed class Settings : CommandSettings
    {
    }
}
