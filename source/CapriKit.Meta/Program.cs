using System.Text.RegularExpressions;
using CapriKit.CommandLine;

namespace CapriKit.Meta;

internal partial class Program
{
    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]    
    private static partial Regex SemVerRegex();
       
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
        if(Help.TryParse(out var help, args))
        {
            if (help.HasCommand)
            {
                if (CommandLineVerbs.AllVerbs.ContainsKey(help.Command))
                {
                    var info = CommandLineVerbs.AllVerbs[help.Command];
                    CommandLineHelp.PrintVerbHelp(info.Item1, info.Item2);
                }
                else
                {
                    Console.WriteLine($"Error, invalid command: {help.Command}");
                }
            }
            else
            {                
                var info = CommandLineVerbs.AllVerbs["help"];
                CommandLineHelp.PrintVerbHelp(info.Item1, info.Item2);                
            }
        }

        if (Bump.TryParse(out var bump, args))
        {

        }
    }

    public static readonly IReadOnlyDictionary<string, (VerbInfo, IReadOnlyList<FlagInfo>)> AllVerbs = new Dictionary<string, (VerbInfo, IReadOnlyList<FlagInfo>)>()
    {
        {CapriKit.Meta.Bump.VerbName, (new VerbInfo(CapriKit.Meta.Bump.VerbName, CapriKit.Meta.Bump.Documentation), CapriKit.Meta.Bump.Flags)}, {CapriKit.Meta.Help.VerbName, (new VerbInfo(CapriKit.Meta.Help.VerbName, CapriKit.Meta.Help.Documentation), CapriKit.Meta.Help.Flags)}
    };
}
