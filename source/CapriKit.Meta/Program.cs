using CapriKit.Meta.Commands;
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
            configure.AddCommand<StreamCommand>("stream");
        });
        app.Run(args);
    }

    private static void Main(string[] args)
    {
        // HACK to fix: https://github.com/spectreconsole/spectre.console/issues/1964
        Console.OutputEncoding = Encoding.UTF8;

#if DEBUG
        if (args.Length == 0)
        {
            RunCommand([]);
            while (true)
            {
                AnsiConsole.Markup(": ");                
                var line = Console.ReadLine() ?? string.Empty;
                var arguments = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                RunCommand(arguments);
                
            }
        }
#endif 
        RunCommand(args);
    }
}
