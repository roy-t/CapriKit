using System.Text.RegularExpressions;
using CapriKit.CommandLine;

namespace CapriKit.Meta;

internal partial class Program
{
    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]    
    private static partial Regex SemVerRegex();
    
    public static void Temp()
    {
        CommandLinePrinter.PrintUsage();        
    }
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            CommandLinePrinter.PrintUsage();
            Console.WriteLine();
            Console.WriteLine("Entering interactive mode, press ctrl+c or type :q to quit");
            Console.WriteLine();
            while (true)
            {
                Console.Write(":> ");
                var line = Console.ReadLine() ?? string.Empty;
                if (line.Equals(":q"))
                {
                    return;
                }                
                Execute(line.Split(null));
            }
        }
        else
        {
            Execute(args);
        }
    }

    private static void Execute(string[] args)
    {
        if(Help.TryParse(out var help, args))
        {
            if (help.HasCommand && CommandLinePrinter.Verbs.Contains(help.Command))
            {
                CommandLinePrinter.PrintVerb(help.Command);
            }
            else
            {
                CommandLinePrinter.PrintVerb("help");
            }
        }

        if (Bump.TryParse(out var bump, args))
        {

        }
    }
}
