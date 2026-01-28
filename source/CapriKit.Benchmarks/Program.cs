using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CapriKit.Benchmarks
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0).AsDefault())
                .AddExporter(JsonExporter.Default);


            var assembly = typeof(Program).Assembly;
            BenchmarkSwitcher.FromAssembly(assembly).Run(args, config);
        }
    }
}


