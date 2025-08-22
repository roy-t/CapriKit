using System.Reflection;
using System.Text;

namespace CapriKit.CommandLine;

public static class CommandLineHelp
{
    private const string Indent = "  ";
    private const string Tab = "    ";

    public static void PrintGeneralHelp(IReadOnlyDictionary<string, (VerbInfo Verb, IReadOnlyList<FlagInfo> Flags)> verbs)
    {
        var name = GetExecutableName();
        var version = GetExecutableVersion();
        Console.WriteLine($"{name} — version {version}");
        Console.WriteLine($"usage: {name} <verb> [<flags>]");
        Console.WriteLine();
        Console.WriteLine("Available verbs:");
        Console.WriteLine();

        PrintAvailableVerbs(verbs);
    }

    public static void PrintAvailableVerbs(IReadOnlyDictionary<string, (VerbInfo Verb, IReadOnlyList<FlagInfo> Flags)> verbs)
    {
        PrintTable(verbs.Values.Select(v => (v.Verb.Name, v.Verb.Documentation)));
    }

    public static void PrintVerbHelp(VerbInfo verb, IReadOnlyList<FlagInfo> flags)
    {
        var name = GetExecutableName();

        Console.WriteLine(verb.Documentation);
        Console.WriteLine();
        Console.WriteLine($"Usage:");
        Console.WriteLine($"{Indent}{name} {verb.Name} [<flags>]");
        Console.WriteLine();
        Console.WriteLine($"Available flags:");
        PrintTable(flags.Select(f => (f.Name, f.Documentation)));
        Console.WriteLine();
    }

    private static void PrintTable(IEnumerable<(string item, string documentation)> table)
    {
        var itemWidth = table.Select(v => v.item.Length).Max();
        var emptyCell = new string(' ', itemWidth + Indent.Length + Tab.Length);

        foreach (var (item, documentation) in table)
        {
            Console.Write($"{Indent}{item.PadRight(itemWidth)}{Tab}");

            if (!string.IsNullOrEmpty(documentation))
            {
                var wrapped = WordWrap(documentation, 80);
                wrapped = wrapped.Replace("\r\n", "\n");
                var lines = wrapped.Split('\n');
                Console.WriteLine(lines[0]);
                foreach (var line in lines.Skip(1))
                {
                    Console.WriteLine($"{emptyCell}{line}");
                }
            }
        }
    }

    public static string WordWrap(string text, int maxLength)
    {
        if (text.Length < maxLength || maxLength < 1)
        {
            return text;
        }

        var builder = new StringBuilder();
        var back = 0;
        var lineLength = 0;
        for (var front = 0; front < text.Length; front++)
        {
            if (char.IsWhiteSpace((text[front])))
            {
                var length = front - back;
                if (lineLength > maxLength)
                {
                    builder.AppendLine();
                    // ignore the whitespace character between this word and the previous word
                    back += 1;
                    lineLength = 0;
                }
                builder.Append(text.Substring(back, length));
                lineLength += length;
                back = front;
            }
        }

        return builder.ToString();
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
