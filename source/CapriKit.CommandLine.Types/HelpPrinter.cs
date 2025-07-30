using System.Reflection;

namespace CapriKit.CommandLine.Types;

public static class HelpPrinter
{    
    private const string Indent = "  ";
    private const string Tab = "    ";

    public static void PrintHeader()
    {
        var name = GetExecutableName();
        var version = GetExecutableVersion();
        Console.WriteLine($"{name} — version {version}");
        Console.WriteLine($"usage: {name} <verb> [<arguments>]");
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine();
    }

    public static void PrintVerbs(params (string verb, string documentation)[] verbs)
    {
        PrintTable(verbs);
    }    

    public static void PrintVerbDetails(string verb, string verbDocumentation,  params (string flag, string flagDocumentation)[] flags)
    {
        var name = GetExecutableName();

        Console.WriteLine($"usage: {name} {verb} [<arguments>]");
        Console.WriteLine();
        PrintTable(flags);
        Console.WriteLine();
    }

    private static void PrintTable(params (string item, string documentation)[] table)
    {
        var itemWidth = table.Select(v => v.item.Length).Max();
        var emptyCell = new string(' ', itemWidth + Indent.Length + Tab.Length);

        foreach (var (item, documentation) in table)
        {
            Console.Write($"{Indent}{item.PadRight(itemWidth)}{Tab}");

            if (!string.IsNullOrEmpty(documentation))
            {
                var lines = documentation.Split('\r', '\n');
                Console.WriteLine(lines[0]);
                foreach (var line in lines.Skip(1))
                {
                    Console.WriteLine($"{emptyCell}{line}");
                }
            }
        }
    }

    private static string GetExecutableName()
    {
        var assemblyName = Assembly.GetEntryAssembly().GetName();
        return assemblyName.Name;
    }

    private static string GetExecutableVersion()
    {
        var assemblyName = Assembly.GetEntryAssembly().GetName();
        // Closest representation of SemVer via the windows API, build is patch
        return $"{assemblyName.Version.Major}.{assemblyName.Version.Minor}.{assemblyName.Version.Build}";
    }
}
