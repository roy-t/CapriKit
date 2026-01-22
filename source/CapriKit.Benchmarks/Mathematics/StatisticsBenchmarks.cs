using BenchmarkDotNet.Attributes;
using CapriKit.Mathematics;

namespace CapriKit.Benchmarks.Mathematics;

public class StatisticsBenchmarks
{
    private double[]? samples;

    [GlobalSetup]
    public void Setup()
    {
        samples = new double[1000];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = Random.Shared.NextDouble();
        }
    }

    [Benchmark]
    public double SampleStandardDeviation()
    {
        return Statistics.SampleStandardDeviation(0.5, samples!);
    }
}
