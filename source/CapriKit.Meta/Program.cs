using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace CapriKit.Meta;

internal partial class Program
{

    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]    
    private static partial Regex SemVerRegex();

    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            while(true)
            {
                Console.Write("meta> ");
                var command  = Console.ReadLine();
                if ("quit".Equals(command))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(command))
                {
                    var commandArray = command.Split(null);
                    ExecuteCommand(commandArray);
                }                                
            }
        }
        else
        {
            ExecuteCommand(args);
        }
    }

    private static void ExecuteCommand(string[] args)
    {
        if (args.Length > 0)
        {
            switch (args[0])
            {
                case "bump":
                    Bump(args[1..]);
                    break;

                default:
                    Error($"Unrecognized argument: {args[0]}");
                    break;
            }
        }
        else
        {
            Console.WriteLine("expected at least 1 argument");
        }
    }

    private static void Bump(string[] args)
    {
        try
        {
            using var stream = File.Open("version.txt", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            var reader = new StreamReader(stream, leaveOpen: true);
            var version = reader.ReadToEnd();
            reader.Dispose();

            if (string.IsNullOrEmpty(version))
            {
                Error("The contents of version.txt should be one SEMVER compatible version number but was empty");
                return;
            }

            var regex = SemVerRegex();
            var match = regex.Match(version);

            // TODO: regex doesn't work??
            if (!match.Success)
            {
                Error($"The contents of version.txt should be one SEMVER compatible version number, but was: {version}");
                return;
            }

            var major = int.Parse(match.Groups["major"].Value);
            var minor = int.Parse(match.Groups["minor"].Value);
            var patch = int.Parse(match.Groups["patch"].Value);
            var pre = match.Groups["prerelease"].Value;
            var build = match.Groups["buildmetadata"].Value;

            if (HasArgument("--major", args))
            {
                major++;
                minor = 0;
                patch = 0;
            }
            else if (HasArgument("--minor", args))
            {
                minor++;
                patch = 0;
            }
            else if (HasArgument("--patch", args))
            {
                patch++;
            }

            if (HasArgumentWithValue("--pre", out var prereleaseTag, args))
            {
                pre = prereleaseTag;
            }

            if (HasArgumentWithValue("--build", out var buildTag, args))
            {
                build = buildTag;
            }

            var nextVersion = ComposeSemanticVersion(major, minor, patch, pre, build);

            Info($"Bumping from {version} to {nextVersion}");
            stream.SetLength(0);
            stream.Seek(0, SeekOrigin.Begin);
            
            using var writer = new StreamWriter(stream);
            {
                writer.WriteLine(nextVersion);
            }                        
        }
        catch (FileNotFoundException fileNotFound)
        {
            Error("File version.txt not found", fileNotFound);
        }        
    }

    private static void Info(string message)
    {
        Console.WriteLine(message);
    }

    private static void Error(string message, Exception? exception = null)
    {
        Console.WriteLine($"[ERROR]: {message}");
        if (exception != null)
        {
            Console.WriteLine(exception);
        }
    }

    private static bool HasArgument(string argument, string[] args)
    {
        return args.Contains(argument);
    }
    
    private static bool HasArgumentWithValue(string argument, [NotNullWhen(true)] out string? value, string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(argument) && (i+1) < args.Length)
            {
                value = args[i + 1];
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string ComposeSemanticVersion(int major, int minor, int patch, string? prerelease = null, string? build = null)
    {
        var builder = new StringBuilder();
        builder.Append(major);
        builder.Append('.');
        builder.Append(minor);
        builder.Append('.');
        builder.Append(patch);

        if(!string.IsNullOrWhiteSpace(prerelease))
        {
            builder.Append('-');
            builder.Append(prerelease);
        }

        if (!string.IsNullOrWhiteSpace(build))
        {
            builder.Append('+');
            builder.Append(build);
        }

        return builder.ToString();
    }
}
