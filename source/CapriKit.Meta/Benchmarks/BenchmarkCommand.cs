using CapriKit.Build;
using CapriKit.Meta.Utilities;
using CapriKit.Meta.Versions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text.Json;

namespace CapriKit.Meta.Benchmarks;

internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var startTime = DateTime.Now;

        var version = VersionUtilities.ReadVersionFromFile() ?? new SemVer(0, 1, 0);
        AnsiConsole.MarkupLineInterpolated($"Running benchmarking for {version}");

        var solutionPath = Config.SolutionPath;
        var solutionDirectory = Config.SolutionDirectory;
        var testResultsDirectory = Config.Outputs.TestDirectory;
        var benchmarkOutputDirectory = Config.Outputs.BenchmarkDirectory;
        var benchmarkDirectory = Config.Assets.BenchmarkDirectory;

        var projectPath = solutionDirectory.Append(@"source\CapriKit.Benchmarks\CapriKit.Benchmarks.csproj");

        using var logger = CommandLogger.CreateBuildLogger();
        var taskList = new TaskList();
        taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        taskList.AddTask("Build Release", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Release));

        var args = new List<string>
        {
            "--artifacts",
            testResultsDirectory,
            "--filter",
            settings.Filter
        };

        if (settings.DryRun)
        {
            args.Add("--job");
            args.Add("Dry");
        }

        taskList.AddTask("Benchmark", DotNetManager.Run(logger.Writer, solutionPath, projectPath, WellKnownConfigurations.Release, args));

        var results = taskList.Execute(logger, cancellationToken);
        if (results.Any(t => t.Exception != null))
        {
            return 1;
        }

        // Find each json file in benchmarkResultsDirectory that is newer than start time
        var directory = new DirectoryInfo(benchmarkOutputDirectory);
        if (!directory.Exists)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"Benchmark report directory does not exist: {benchmarkOutputDirectory}");
            return 1;
        }

        var entries = ParseBenchmarkResults(version, startTime, benchmarkOutputDirectory);
        if (entries.Count == 0)
        {
            AnsiConsoleExt.ErrorMarkupLineInterpolated($"No up-to-date benchmark entries found in: {benchmarkOutputDirectory}");
            return 1;
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"Found {entries.Count} benchmark entries in: {benchmarkOutputDirectory}");
        }

        BenchmarkPrinter.PrintBenchmarkResults($"Results ({entries.Count})", entries);

        var latestBenchmarkedVersion = BenchmarkRepository.List().Max();
        if (latestBenchmarkedVersion != null && latestBenchmarkedVersion != version)
        {
            var before = BenchmarkRepository.Load(latestBenchmarkedVersion);
            AnsiConsole.MarkupLineInterpolated($"Comparing {latestBenchmarkedVersion} to current run ({version})");
            var difference = BenchmarkDiffer.ComputeDifference(before, entries);
            BenchmarkPrinter.PrintBenchmarkDiff(difference);
        }

        if (settings.DryRun == false && settings.Filter == Settings.FilterIncludeAll)
        {
            var exists = BenchmarkRepository.Exists(version);
            if (!exists || (exists && AnsiConsole.Confirm($"Overwrite results for {version}?", false)))
            {
                AnsiConsole.MarkupLine("Storing results..");
                BenchmarkRepository.Save(version, entries);
            }
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"Discarding results of unrepresentative run (--filter {settings.Filter} --dry-run {settings.DryRun})");
        }

        return 0;
    }

    private static IReadOnlyList<BenchmarkEntry> ParseBenchmarkResults(SemVer version, DateTime timestamp, string directory)
    {
        var entries = new List<BenchmarkEntry>();
        var info = new DirectoryInfo(directory);
        foreach (var file in info.GetFiles("*.json"))
        {
            if (file.LastWriteTime < timestamp) { continue; }

            using var stream = file.OpenRead();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var benchmarkResults = JsonSerializer.Deserialize<BenchmarkResults>(json);
            if (benchmarkResults == null) { continue; }

            foreach (var result in benchmarkResults.Benchmarks)
            {
                // Even though these values are also calculated by BenchmarkDotNet we recalculate them using our own code
                // to prevent numbers from being slightly different due to compound errors and rounding differences.
                var mean = Mathematics.Statistics.Mean(result.Statistics.OriginalValues);
                var deviation = Mathematics.Statistics.SampleStandardDeviation(mean, result.Statistics.OriginalValues);
                var error = Mathematics.Statistics.StandardError(deviation, result.Statistics.OriginalValues.Length);
                var sampleCount = result.Statistics.OriginalValues.Length;
                entries.Add(new BenchmarkEntry(version, timestamp, result.FullName, mean, error, deviation, sampleCount));
            }
        }

        return entries;
    }

    public sealed class Settings : CommandSettings
    {
        public const string FilterIncludeAll = "*";

        [Description("Benchmarks to run, defaults to '*' for all")]
        [CommandOption("--filter", false)]
        public string Filter { get; init; } = FilterIncludeAll;

        [Description("Performs a dry run of the benchmark(s) to test if they work")]
        [CommandOption("--dry-run", false)]
        public bool DryRun { get; init; }
    }
}
