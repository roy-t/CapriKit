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
        try
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
                    Console.WriteLine($"Invalid verb: {args[0]}");
                    Console.WriteLine();
                    Console.WriteLine("Available verbs:");
                    Console.WriteLine();
                    CommandLineHelp.PrintAvailableVerbs(CommandLineVerbs.AllVerbs);
                    break;
            }
        }
        catch(UnmatchedFlagsException unmatched)
        {
            WriteError(unmatched.Message);
            Console.WriteLine($"Use 'help --command {unmatched.Verb}' to read more about the supported arguments");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            WriteException("An unexpected error occured", ex);
            Console.WriteLine();
        }
    }

    private static void WriteException(string message, Exception ex)
    {
        if (string.IsNullOrEmpty(ex.StackTrace))
        {
            WriteError($"{message}: {ex.Message}");
        }
        else
        {
            WriteError($"{message}: {ex.Message + Environment.NewLine + ex.StackTrace}");
        }
    }

    private static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("Error: ");
        Console.ResetColor();
        Console.WriteLine(message);        
    }
}
