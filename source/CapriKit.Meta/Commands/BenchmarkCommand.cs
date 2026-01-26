using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace CapriKit.Meta.Commands;

// TODO: Ensure that the test filters can be passed on so that you can run a single test or single suite

internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        var (solutionPath, _, testResultsDirectory, _, benchmarkResultsDirectory, documentationDirectory) = BuildUtilities.GatherBuildInputs();
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
                // Even though these values are also calculated by BenchmarkDotNet we recalculate them using our own code
                // to prevent numbers from being slightly different due to compound errors and rounding differences.
                var mean = Mathematics.Statistics.Mean(result.Statistics.OriginalValues);
                var deviation = Mathematics.Statistics.SampleStandardDeviation(mean, result.Statistics.OriginalValues);
                var error = Mathematics.Statistics.StandardError(deviation, result.Statistics.OriginalValues.Length);
                                
                var nameColumn = result.FullName;
                var testColumn = result.MethodTitle;
                var meanColumn = $"{mean:F3} ns";
                var errorColumn = $"{error:F4} ns";
                var deviationColumn = $"{deviation:F4} ns";
                table.AddRow(nameColumn, testColumn, meanColumn, errorColumn, deviationColumn);
            }

            AnsiConsole.Write(table);
        }

        // TODO: compare benchmark results with latest benchmark results and ask the user to 'promote' them if they are significantly different
        // if we want to do this correctly this means doing a Welch two-sample t-test
        // for example: https://github.com/accord-net/framework/blob/development/Sources/Accord.Statistics/Testing/TwoSample/TwoSampleTTest.cs#L195
        // note variance = (standard deviation)^2

        // TODO: Store them for each version of CapriKit in the documentation folder thing and ask to overwrite if the file exist
        // Display significant differences compared to the last 2 versions (or the current version and prev version if there is already a CURRENTVERSION.json file.
        return TaskList.ExitCodeFromResult(results);
    }

    public sealed class Settings : CommandSettings
    {
    }
}
