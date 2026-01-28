using CapriKit.Build;
using CapriKit.Meta.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace CapriKit.Meta.Benchmarks;

// TODO: Ensure that the test filters can be passed on so that you can run a single test or single suite

internal sealed class BenchmarkCommand : Command<BenchmarkCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        //var startTime = DateTime.Now;
        var startTime = DateTime.MinValue;

        var version = VersionUtilities.ReadVersionFromFile() ?? new SemVer(0, 1, 0);
        AnsiConsole.MarkupLineInterpolated($"Running benchmarking for {version}");

        var solutionPath = Config.SolutionPath;
        var solutionDirectory = Config.SolutionDirectory;
        var testResultsDirectory = Config.Outputs.TestDirectory;
        var benchmarkOutputDirectory = Config.Outputs.BenchmarkDirectory;
        var benchmarkDirectory = Config.Assets.BenchmarkDirectory;
                
        //var projectPath = Path.Combine(solutionDirectory, @"source\CapriKit.Benchmarks\CapriKit.Benchmarks.csproj");

        //using var logger = BuildLogger.CreateBuildLogger();
        //var taskList = new TaskList();
        //taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        //taskList.AddTask("Build Release", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Release));
        //taskList.AddTask("Benchmark", DotNetManager.Run(logger.Writer, solutionPath, projectPath, WellKnownConfigurations.Release, testResultsDirectory));

        //var results = taskList.Execute(logger, cancellationToken);
        //if (results.Any(t => t.Exception != null))
        //{
        //    return 1;
        //}

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
            AnsiConsoleExt.InfoMarkupLineInterpolated($"Found {entries.Count} benchmark entries in: {benchmarkOutputDirectory}");
        }

        PrintBenchmarkResults($"Results ({entries.Count})", entries);

        var latestBenchmarkedVersion = GetLatestBenchmarkResults(benchmarkDirectory);
        if (latestBenchmarkedVersion != null && latestBenchmarkedVersion != version)
        {
            var before =   BenchmarkRepository.Load(latestBenchmarkedVersion);
            AnsiConsole.MarkupLineInterpolated($"Comparing {latestBenchmarkedVersion} to current run ({version})");
            PrintBenchmarkDiff(before, entries);
        }

        // TODO: only ask if already exists
        if (AnsiConsole.Confirm($"Overwrite results for {version}?", false))
        {
            AnsiConsole.MarkupLine("Storing results..");
            BenchmarkRepository.Save(version, entries);
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

    private static void PrintBenchmarkResults(string title, IReadOnlyList<BenchmarkEntry> entries)
    {
        var table = new Table();
        table.Title(title);
        table.AddColumn("Id");
        table.AddColumn("Mean");
        table.AddColumn("StdError");
        table.AddColumn("StdDev");

        foreach (var entry in entries)
        {
            var nameColumn = entry.Id;
            var meanColumn = $"{entry.Mean:F3} ns";
            var errorColumn = $"{entry.StandardError:F4} ns";
            var deviationColumn = $"{entry.StandardDeviation:F4} ns";
            table.AddRow(nameColumn, meanColumn, errorColumn, deviationColumn);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private void PrintBenchmarkDiff(IReadOnlyList<BenchmarkEntry> previousBenchmark, IReadOnlyList<BenchmarkEntry> currentBenchmark)
    {
        var previousDict = previousBenchmark.ToDictionary(e => e.Id);
        var currentDict = currentBenchmark.ToDictionary(e => e.Id);

        var removedKeys = previousDict.Keys.Except(currentDict.Keys);
        var removedTest = removedKeys.Select(k => previousDict[k]).ToList();

        var addedKeys = currentDict.Keys.Except(previousDict.Keys);
        var addedTests = addedKeys.Select(k => currentDict[k]).ToList();

        
        if (removedTest.Count > 0)
        {
            PrintBenchmarkResults($"Removed ({removedTest.Count})", removedTest);
        }

        if (addedTests.Count > 0)
        {
            PrintBenchmarkResults($"Added ({addedTests.Count})", addedTests);
        }

        var changedBenchmarks = new List<(BenchmarkEntry before, BenchmarkEntry after)>();
        var comparableKeys = previousDict.Keys.Intersect(currentDict.Keys);

        foreach (var key in comparableKeys)
        {
            var prev = previousDict[key];
            var curr = currentDict[key];

            var t = Mathematics.StudentTTest.ForIndependentSamples(prev.Mean, prev.StandardDeviation, prev.SampleCount, curr.Mean, curr.StandardDeviation, curr.SampleCount);
            var dof = Mathematics.StudentTTest.GetDegreesOfFreedom(prev.StandardDeviation, prev.SampleCount, curr.StandardDeviation, curr.SampleCount);
            var probability = Mathematics.StudentTTest.ComputeTwoTailedProbabilityOfT(t, dof);
            if (probability < 0.05)
            {
                changedBenchmarks.Add((prev, curr));
            }
        }
        
        if (changedBenchmarks.Count > 0)
        {
            var table = new Table();
            table.Title($"Significantly different ({changedBenchmarks.Count})");
            table.AddColumn("Id");
            table.AddColumn("Old Mean");
            table.AddColumn("New Mean");
            table.AddColumn("Diff");

            foreach (var (before, after) in changedBenchmarks)
            {
                var id = new Text(after.Id);
                var oldMean = new Text($"{before.Mean:F3} ns");
                var newMean = new Text($"{after.Mean:F3} ns");

                var percentage = 100.0 - (Math.Min(before.Mean, after.Mean) / Math.Max(before.Mean, after.Mean)) * 100;
                var diff = after.Mean < before.Mean
                    ? new Text($"-{percentage:F2}%", new Style(Color.Green))
                    : new Text($"+{percentage:F2}%", new Style(Color.Red));

                table.AddRow(id, oldMean, newMean, diff);
            }
            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }   

    private static SemVer? GetLatestBenchmarkResults(string path)
    {
        // TODO: use Reposiory.List().Max();
        var info = new DirectoryInfo(path);
        SemVer? latest = null;
        foreach (var file in info.GetFiles("*.json"))
        {
            try
            {
                var version = SemVer.Parse(file.Name[0..^5]);
                if (latest == null || version > latest)
                {
                    latest = version;
                }
            }
            catch { }
        }
        return latest;
    }   

    private static string GetPathToResult(string benchmarkDirectory, SemVer version)
    {
        return Path.Combine(benchmarkDirectory, $"{version}.json");
    }

    public sealed class Settings : CommandSettings
    {
    }
}


