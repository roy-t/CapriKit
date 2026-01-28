namespace CapriKit.Meta.Benchmarks;

internal record BenchmarkDifference(IReadOnlyList<BenchmarkEntry> Added, IReadOnlyList<BenchmarkEntry> Removed, IReadOnlyList<(BenchmarkEntry before, BenchmarkEntry after)> Changed);

internal static class BenchmarkDiffer
{
    public static BenchmarkDifference ComputeDifference(IReadOnlyList<BenchmarkEntry> previousBenchmark, IReadOnlyList<BenchmarkEntry> currentBenchmark)
    {
        var previousDict = previousBenchmark.ToDictionary(e => e.Id);
        var currentDict = currentBenchmark.ToDictionary(e => e.Id);

        var removedKeys = previousDict.Keys.Except(currentDict.Keys);
        var removedTest = removedKeys.Select(k => previousDict[k]).ToList();

        var addedKeys = currentDict.Keys.Except(previousDict.Keys);
        var addedTests = addedKeys.Select(k => currentDict[k]).ToList();

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

        return new BenchmarkDifference(addedTests, removedTest, changedBenchmarks);
    }
}
