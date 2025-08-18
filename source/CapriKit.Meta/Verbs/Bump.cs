using CapriKit.CommandLine;
using System.Text.RegularExpressions;

namespace CapriKit.Meta.Verbs;

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
    /// 
    /// A pre-release version MAY be denoted by appending a hyphen and a series of dot separated identifiers immediately following the patch version.Identifiers MUST comprise only ASCII alphanumerics and hyphens[0 - 9A - Za - z -]. Identifiers MUST NOT be empty.Numeric identifiers MUST NOT include leading zeroes.Pre-release versions have a lower precedence than the associated normal version.A pre-release version indicates that the version is unstable and might not satisfy the intended compatibility requirements as denoted by its associated normal version.Examples: 1.0.0-alpha, 1.0.0-alpha.1, 1.0.0-0.3.7, 1.0.0-x.7.z.92, 1.0.0-x-y-z.--.
    /// </summary>
        [Flag("--prerelease")]
    public partial string Prerelease { get; }

    /// <summary>
    /// Set the build meta data
    ///
    /// Build metadata MAY be denoted by appending a plus sign and a series of dot separated identifiers immediately following the patch or pre-release version. Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-]. Identifiers MUST NOT be empty. Build metadata MUST be ignored when determining version precedence. Thus two versions that differ only in the build metadata, have the same precedence. Examples: 1.0.0-alpha+001, 1.0.0+20130313144700, 1.0.0-beta+exp.sha.5114f85, 1.0.0+21AF26D3----117B344092BD.
    /// </summary>
    [Flag("--build-meta-data")]
    public partial string BuildMetaData { get; }


    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVerRegex();

    public static void Execute(params string[] args)
    {
        // TODO: see if this should be a unit test instead, or we should wait until the real test functionality is built.
        var text = "1.4.3-p-r-e+HASH";// File.ReadAllText(filename);
        var match = SemVerRegex().Match(text);
        if (match.Success)
        {
            var major = match.Groups[1];
            var minor = match.Groups[2];
            var patch = match.Groups[3];
            var pre = match.Groups[4];
            var build = match.Groups[5];
        }        
    }
}
