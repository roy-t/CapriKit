using System.Text.RegularExpressions;
using CapriKit.CommandLine;
using CapriKit.Meta.Verbs;

namespace CapriKit.Meta;

internal partial class Program
{           
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            CommandLineHelp.PrintGeneralHelp(CommandLineVerbs.AllVerbs);
                   
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
        switch (args[0])
        {
            case Help.VerbName:
                Help.Execute(args);
                break;

            case Bump.VerbName:
                Bump.Execute(args);
                break;

            default:
                Console.WriteLine("Invalid arguments: " + string.Join(", ", args));
                Console.WriteLine();
                Console.WriteLine("Available commands:");
                Console.WriteLine();
                CommandLineHelp.PrintAvailableCommands(CommandLineVerbs.AllVerbs);
                break;
        }        
    }
}
