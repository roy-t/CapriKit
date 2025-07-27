using CapriKit.CommandLine.Types;
using System.Diagnostics;

namespace CapriKit.Meta;


/// <verb>Lalalalal</verb>
/// <summary>
/// Bumps the package version, in line with semantic versioning 2.0
/// </summary>
[Verb("bump")]
public partial class Bump
{
    /// <summary>
    /// Increase the major version of the package
    /// </summary>
    [Flag("--major")]
    public partial bool Major { get; }

    /// <summary>
    /// Increase the minor version of the package
    /// </summary>
    [Flag("--minor")]
    public partial bool Minor { get; }

    /// <summary>
    /// Increase the patch version of the package
    /// </summary>
    [Flag("--patch")]
    public partial bool Patch { get; }

    /// <summary>
    /// Set the prerelease information
    /// </summary>
    [Flag("--prerelease")]
    public partial string Prerelease { get; }

    /// <summary>
    /// Set the build meta data
    /// </summary>
    [Flag("--build-meta-data")]
    public partial string BuildMetaData { get; }
}

// TODO: generate something like this:

//public partial class Bump
//{
//    private bool major;
//    public partial bool Major { get => this.major; }

//    public static Bump? Parse(string[] args)
//    {
//        return null;
//    }
//}

public class FooBar
{
    public static Bump? Parse(params string[] args)
    {
        if (ArgsParser.IsVerb("bump", args))
        {
            var hasMajor = ArgsParser.TryParseFlag<bool>("--major", out bool major, args);
            var hasMinor = ArgsParser.TryParseFlag<bool>("--minor", out bool minor, args);

            //return new Bump(m)

        }

        return null;
    }
}

// Generate
public class BVerbExecuter : AVerbExecutor
{
    public static void Create()
    {
        // Generators addsss

        var executer = new BVerbExecuter();
        executer.Verbs.Add("a");
        executer.VerbToFlagToDocs.Add("b", []);
        //etc..
    }

    // Generate Per method execute things

    // Performs the given action if the arguments match this verb
    public void Execute(Action<Bump> onBump, params string[] args)
    {
        //var bump = Bump.Parse(args);
        //if (bump != null)
        //{
        //    onBump(bump);
        //}
    }
}
