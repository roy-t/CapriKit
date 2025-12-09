using BenchmarkDotNet.Attributes;

namespace CapriKit.Benchmarks;

public class TestBenchmark
{
    [Benchmark]
    public int GenerateRandomNumber()
    {
        return Random.Shared.Next();
    }
}
