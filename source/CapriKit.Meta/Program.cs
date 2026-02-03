using CapriKit.Meta.Benchmarks;
using CapriKit.Meta.Builds;
using CapriKit.Meta.Versions;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text;

namespace CapriKit.Meta;

internal partial class Program
{
    private static void RunCommand(string[] args)
    {
        var app = new CommandApp();
        app.Configure(configure =>
        {
            configure.AddCommand<BumpCommand>("bump")
                .WithDescription("Bumps the package version, in line with semantic versioning 2.0");
            configure.AddCommand<ReleaseCommand>("release")
                .WithDescription("Runs formatting, test, build and pack steps before pushing to NuGet");
            configure.AddCommand<TestCommand>("test")
                .WithDescription("Run all unit tests");
            configure.AddCommand<BenchmarkCommand>("benchmark")
                .WithDescription("Run all unit benchmarks");
        });
        app.Run(args);
    }

    private static void Main(string[] args)
    {
        // Set the correct console output decoding so that Spectre.Console can render
        // emoji and other glyphs. On Windows this does require that users use Windows Terminal
        // https://github.com/spectreconsole/spectre.console/issues/1964
        Console.OutputEncoding = Encoding.UTF8;
        RunCommand(args);
        if (args.Length == 0)
        {
            Console.WriteLine();
            while (true)
            {
                AnsiConsole.Markup("> ");
                var line = Console.ReadLine() ?? string.Empty;
                var arguments = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                RunCommand(arguments);
            }
        }
    }
}
