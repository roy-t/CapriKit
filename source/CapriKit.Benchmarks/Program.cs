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
            var outputPath = "./../../tst";
            if (args.Length == 1)
            {
                if (IsValidPath(args[0]))
                {
                    outputPath = args[0];
                }
                else
                {
                    throw new ArgumentException($"Invalid path: {args[0]}");
                }
            }

            var config = DefaultConfig.Instance
                .AddJob(Job.Default.WithRuntime(CoreRuntime.Core10_0))                
                .AddExporter(JsonExporter.Default)
                .WithArtifactsPath(outputPath);
           
            BenchmarkRunner.Run<TestBenchmark>(config);
        }

        static bool IsValidPath(string directory)
        {
            var invalid = Path.GetInvalidPathChars()
                .Any(directory.Contains) || directory.Equals("con", StringComparison.OrdinalIgnoreCase);
            return !invalid;
        }
    }   
}

    
