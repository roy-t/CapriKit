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
        //var startTime = DateTime.Now;
        var startTime = DateTime.MinValue;

        var version = VersionUtilities.ReadVersionFromFile() ?? new SemVer(0, 1, 0);
        AnsiConsole.MarkupLineInterpolated($"Running benchmarking for {version}");

        var (solutionPath, _, testResultsDirectory, _, benchmarkOutputDirectory, benchmarkDirectory) = BuildUtilities.GatherBuildInputs();
        var solutionDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;
        var projectPath = Path.Combine(solutionDirectory, @"source\CapriKit.Benchmarks\CapriKit.Benchmarks.csproj");

        //using var logger = BuildUtilities.CreateBuildLogger();
        //var taskList = new TaskList();
        //taskList.AddTask("Restore", DotNetManager.Restore(logger.Writer, solutionPath));
        //taskList.AddTask("Build Release", DotNetManager.Build(logger.Writer, solutionPath, WellKnownConfigurations.Release));
        //taskList.AddTask("Benchmark", DotNetManager.Run(logger.Writer, solutionPath, projectPath, WellKnownConfigurations.Release, testResultsDirectory));

        //var results = taskList.Execute(cancellationToken);
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

        PrintBenchmarkResults(entries);

        var latestBenchmarkedVersion = GetLatestBenchmarkResults(benchmarkDirectory);
        if (latestBenchmarkedVersion != null && latestBenchmarkedVersion != version)
        {
            var before = ReadBenchmarkResults(benchmarkDirectory, latestBenchmarkedVersion);
            PrintBenchmarkDiff(before, entries);
        }

        StoreBenchmarkResults(benchmarkDirectory, version, entries);
        var foo = ReadBenchmarkResults(benchmarkDirectory, version);

        // TODO: compare benchmark results with latest benchmark results and ask the user to 'promote' them if they are significantly different
        // if we want to do this correctly this means doing a Welch two-sample t-test
        // for example: https://github.com/accord-net/framework/blob/development/Sources/Accord.Statistics/Testing/TwoSample/TwoSampleTTest.cs#L195
        // note variance = (standard deviation)^2

        // TODO: Store them for each version of CapriKit in the documentation folder thing and ask to overwrite if the file exist
        // Display significant differences compared to the last 2 versions (or the current version and prev version if there is already a CURRENTVERSION.json file.
        //return TaskList.ExitCodeFromResult(results);
        return 0;
    }

    private static IReadOnlyList<BenchmarkResultEntry> ParseBenchmarkResults(SemVer version, DateTime timestamp, string directory)
    {
        var entries = new List<BenchmarkResultEntry>();
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
                entries.Add(new BenchmarkResultEntry(version, timestamp, result.FullName, mean, error, deviation, sampleCount));
            }
        }

        return entries;
    }

    private static void PrintBenchmarkResults(IReadOnlyList<BenchmarkResultEntry> entries)
    {
        var table = new Table();
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
    }

    private void PrintBenchmarkDiff(IReadOnlyList<BenchmarkResultEntry> previousBenchmark, IReadOnlyList<BenchmarkResultEntry> currentBenchmark)
    {
        var previousDict = previousBenchmark.ToDictionary(e => e.Id);
        var currentDict = currentBenchmark.ToDictionary(e => e.Id);

        var removedKeys = previousDict.Keys.Except(currentDict.Keys);
        var removedTest = removedKeys.Select(k => previousDict[k]).ToList();

        var addedKeys = currentDict.Keys.Except(previousDict.Keys);
        var addedTests = addedKeys.Select(k => currentDict[k]).ToList();

        AnsiConsole.MarkupLineInterpolated($"Removed {removedTest.Count} benchmarks");
        PrintBenchmarkResults(removedTest);

        AnsiConsole.MarkupLineInterpolated($"Added {addedTests.Count} benchmarks");
        PrintBenchmarkResults(addedTests);

        var changedBenchmarks = new List<(BenchmarkResultEntry b, BenchmarkResultEntry a)>();
        var comparableKeys = previousDict.Keys.Union(currentDict.Keys);

        foreach(var key in comparableKeys)
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

        AnsiConsole.MarkupLineInterpolated($"Signicantly changed {addedTests.Count()} benchmarks");

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Old Mean");
        table.AddColumn("New Mean");
        table.AddColumn("Diff");

        // TODO: create diff
        throw new Exception("TODO: create diff");
        AnsiConsole.Write(table);
    }

    private static void StoreBenchmarkResults(string benchmarkDirectory, SemVer version, IReadOnlyList<BenchmarkResultEntry> entries)
    {
        var path = GetPathToResult(benchmarkDirectory, version);
        var info = new FileInfo(path);
        if (info.Exists)
        {
            throw new Exception("File Exists");
        }
        else
        {
            Directory.CreateDirectory(info.DirectoryName!);
            using var stream = info.Create();
            JsonSerializer.Serialize(stream, entries);
        }
    }

    private static SemVer? GetLatestBenchmarkResults(string path)
    {
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

    private static IReadOnlyList<BenchmarkResultEntry> ReadBenchmarkResults(string benchmarkDirectory, SemVer version)
    {
        var path = GetPathToResult(benchmarkDirectory, version);
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException(null, path);
        }
        else
        {
            using var stream = info.OpenRead();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var benchmarkResults = JsonSerializer.Deserialize<IReadOnlyList<BenchmarkResultEntry>>(json);
            if (benchmarkResults == null)
            {
                throw new Exception($"Failed to parse benchmark result entries in file {path}");
            }

            return benchmarkResults;
        }
    }

    private static string GetPathToResult(string benchmarkDirectory, SemVer version)
    {
        return Path.Combine(benchmarkDirectory, $"{version}.json");
    }

    public sealed class Settings : CommandSettings
    {
    }
}

public record BenchmarkResultEntry(SemVer Version, DateTime timestamp, string Id, double Mean, double StandardError, double StandardDeviation, int SampleCount);
