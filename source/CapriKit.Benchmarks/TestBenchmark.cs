using BenchmarkDotNet.Attributes;

namespace CapriKit.Benchmarks;

public class TestBenchmark
{
    [Benchmark]
    public int GenerateRandomIntegers()
    {
        return Random.Shared.Next();
    }

    [Benchmark]
    public float GenerateRandomFloats()
    {
        return Random.Shared.NextSingle();
    }
}
