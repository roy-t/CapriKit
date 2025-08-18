using CapriKit.CommandLine;

namespace CapriKit.Meta.Verbs;

/// <summary>
/// Displays the help information
/// </summary>
[Verb("help")]
public partial class Help
{
    /// <summary>
    /// Specifies the command to show help information for
    /// </summary>
    [Flag("--command")]
    public partial string Command { get; }

    public static void Execute(params string[] args)
    {
        var help = Parse(args);
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
}
