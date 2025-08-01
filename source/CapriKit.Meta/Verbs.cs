using CapriKit.CommandLine;
using System;
using System.Text;

namespace CapriKit.Meta;

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
}

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


    public static readonly IReadOnlyDictionary<string, string> FlagDocumentation = new Dictionary<string, string>()
    {
        { "a", "b" },
        { "c", "b" }
    };
}
