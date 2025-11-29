using CapriKit.Meta.Utilities;
using Spectre.Console.Cli;

namespace CapriKit.Meta.Commands;

internal sealed class TestCommand : Command<TestCommand.Settings>
{
    public override int Execute(CommandContext context, Settings settings)
    {
        //using var stream = FileRotator.CreateFile(@"C:\Users\Roy-T\Desktop", "log-file", ".log", 3);
        //using var writer = new StreamWriter(stream);
        //writer.WriteLine("Hello World!");
        return 0;
    }
    public sealed class Settings : CommandSettings
    {
    }
}
