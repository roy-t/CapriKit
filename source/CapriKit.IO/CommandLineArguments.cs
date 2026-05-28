namespace CapriKit.IO;

/// <summary>
/// Utilities for parsing command line arguments
/// </summary>
public static class CommandLineArguments
{
    public static bool IsPresent(string argument)
    {
        var args = Environment.GetCommandLineArgs();
        return args.Any(a => a.Equals(argument, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsNotPresent(string argument)
    {
        return !IsPresent(argument);
    }

    public static string GetArgumentValueOrDefault(string argument, string @default)
    {
        var value = GetArgumentValue(argument);
        if (string.IsNullOrEmpty(value))
        {
            return @default;
        }

        return value;
    }

    public static string GetArgumentValue(string argument)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(argument, StringComparison.OrdinalIgnoreCase))
            {
                return Unquote(args[i + 1]);
            }
        }

        return string.Empty;
    }

    private static string Unquote(string value)
    {
        if (value.StartsWith('"') && value.EndsWith('"'))
        {
            return value[1..^1];
        }

        return value;
    }
}
