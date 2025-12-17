using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
using static CapriKit.Build.MSBuildManager;

namespace CapriKit.Meta.Commands;

internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MSBuildManager.InitializeMsBuild();
        var startTime = DateTime.Now;

        var (solutionPath, _, testResultsDirectory, _, benchmarkResultsFileName) = BuildUtilities.GatherBuildInputs();
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var projectPath = Path.Combine(solutionDirectory, @"source\CapriKit.Benchmarks\CapriKit.Benchmarks.csproj");

        using var logger = BuildUtilities.CreateBuildLogger();
        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Build Release", MSBuildManager.BuildSolution(logger.Writer, solutionPath, WellKnownConfigurations.Release, WellKnownTargets.Build));
        taskList.AddTask("Benchmark", DotNetManager.Run(logger.Writer, solutionPath, projectPath, WellKnownConfigurations.Release, testResultsDirectory));

        var results = taskList.Execute(cancellationToken);

        var testFile = new FileInfo(benchmarkResultsFileName);
        if (!testFile.Exists)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Benchmark report file does not exist: {benchmarkResultsFileName}");
            return 1;
        }

        if (testFile.LastWriteTime < startTime)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Benchmark report file is out of date: {benchmarkResultsFileName}");
            return 1;
        }

        var json = File.ReadAllText(benchmarkResultsFileName);
        var benchmarkResults = JsonSerializer.Deserialize<BenchmarkResults>(json)!;

        // TODO: use the new math/statistics stuff to ensure we always compute the SD and Mean and .. in the same way
        //if (benchmarkResults != null)
        //{
        //    var mean = CapriKit.Mathematics.Statistics.Mean(benchmarkResults.Benchmarks[0].Statistics.OriginalValues.Select(f => (double)f);
        //}


        var table = new Table();
        table.AddColumn("Class");
        table.AddColumn("Test");
        table.AddColumn("Mean");
        table.AddColumn("Margin of Error"); // 99.9% sure the real mean is the estimated margin plus or minus this error
        table.AddColumn("StdDev");

        foreach (var result in benchmarkResults.Benchmarks)
        {
            var clazz = result.Type;
            var test = result.MethodTitle;
            var mean = $"{result.Statistics.Mean:F3} ns";
            var error = $"{result.Statistics.StandardError:F4} ns";
            var deviation = $"{result.Statistics.StandardDeviation:F4} ns";
            table.AddRow(clazz, test, mean, error, deviation);
        }

        AnsiConsole.Write(table);

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
