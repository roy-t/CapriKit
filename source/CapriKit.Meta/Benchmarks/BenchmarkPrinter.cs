using Spectre.Console;

namespace CapriKit.Meta.Benchmarks;

internal static class BenchmarkPrinter
{
    public static void PrintBenchmarkResults(string title, IReadOnlyList<BenchmarkEntry> entries)
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

    public static void PrintBenchmarkDiff(BenchmarkDifference difference)
    {
        var (added, removed, changed) = difference;

        if (removed.Count > 0)
        {
            PrintBenchmarkResults($"Removed ({removed.Count})", removed);
        }

        if (added.Count > 0)
        {
            PrintBenchmarkResults($"Added ({added.Count})", added);
        }

        if (changed.Count > 0)
        {
            var table = new Table();
            table.Title($"Significantly different ({changed.Count})");
            table.AddColumn("Id");
            table.AddColumn("Old Mean");
            table.AddColumn("New Mean");
            table.AddColumn("Diff");

            foreach (var (before, after) in changed)
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
}
