using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace CapriKit.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {            
            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0))
                .AddValidator(ExecutionValidator.FailOnError)
                .AddLogger(ConsoleLogger.Unicode)
                .AddExporter(JsonExporter.Default)
                .WithArtifactsPath("./../../tst");

            var result = BenchmarkRunner.Run<TestBenchmark>(config);
        }
    }
}
