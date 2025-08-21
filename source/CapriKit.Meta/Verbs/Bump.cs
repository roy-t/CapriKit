using CapriKit.CommandLine;
using System.Diagnostics;
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

    public static void Execute(params string[] args)
    {
        // TODO: read previous version from disk or assume 0.1;
        var text = "1.4.3-p-r-e+HASH";
        var version = SemVer.Parse(text);

        Console.Write($"Version change: {version} -> ");

        var bump = Parse(args);
        if (bump.HasMajor && bump.Major)
        {
            version = version.BumpMajor();
        }

        if (bump.HasMinor && bump.Minor)
        {
            version = version.BumpMinor();
        }

        if (bump.HasPatch && bump.Patch)
        {
            version = version.BumpPatch();
        }

        if (bump.HasPrerelease)
        {
            version = version.WithPreReleaseData(bump.Prerelease);
        }

        if (bump.HasBuildMetaData)
        {
            version = version.WithBuildMetaData(bump.BuildMetaData);
        }


        Console.WriteLine($"{version}");
    }
}

// TODO: move to separate library
public partial class SemVer
{    
    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVerRegex();

    [GeneratedRegex("^[a-zA-Z0-9-]+$")]
    private static partial Regex IdentifierRegex();

    public SemVer(int major, int minor, int patch, string? preRelease = null, string? buildMetaData = null)
    {
        if (major < 0)
        {
            throw new ArgumentException("Major version should be positive");
        }

        if (minor < 0)
        {
            throw new ArgumentException("Minor version should be positive");
        }

        if (patch < 0)
        {
            throw new ArgumentException("Patch version should be positive");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
        
        ValidateTextPart(preRelease);
        PreRelease = preRelease ?? string.Empty;

        ValidateTextPart(buildMetaData);
        BuildMetaData = buildMetaData ?? string.Empty;        
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string PreRelease { get; }
    public string BuildMetaData { get; }

    public SemVer BumpMajor()
    {
        return new SemVer(Major + 1, Minor, Patch, PreRelease, BuildMetaData);        
    }

    public SemVer BumpMinor()
    {
        return new SemVer(Major, Minor + 1, Patch, PreRelease, BuildMetaData);
    }

    public SemVer BumpPatch()
    {
        return new SemVer(Major, Minor, Patch + 1, PreRelease, BuildMetaData);
    }

    public SemVer WithPreReleaseData(string? preRelease)
    {        
        return new SemVer(Major, Minor, Patch, preRelease, BuildMetaData);
    }

    public SemVer WithBuildMetaData(string? buildMetaData)
    {
        
        return new SemVer(Major, Minor, Patch, PreRelease, buildMetaData);
    }

    public static SemVer Parse(string text)
    {
        // TODO: add tests
        var match = SemVerRegex().Match(text);
        if (match.Success)
        {
            var majorString = match.Groups[1].Value;
            var minorString = match.Groups[2].Value;
            var patchString = match.Groups[3].Value;
            var preRelease = match.Groups[4].Value;
            var buildMetaData = match.Groups[5].Value;

            if (!int.TryParse(majorString, out var major))
            {
                throw new Exception($"The major segment in version {text} should be a positive integer, but was: {major}");
            }

            if (!int.TryParse(minorString, out var minor))
            {
                throw new Exception($"The minor segment in version {text} should be a positive integer, but was: {minor}");
            }

            if (!int.TryParse(patchString, out var patch))
            {
                throw new Exception($"The patch segment in version {text} should be a positive integer, but was: {patch}");
            }

            return new SemVer(major, minor, patch, preRelease, buildMetaData);
        }
        else
        {
            throw new Exception($"Version '{text}' does not match the pattern for a version that complies with the semantic versioning 2.0 specification");
        }
    }

    public override string ToString()
    {
        var text = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease))
        {
            text = text + "-" + PreRelease;
        }

        if (!string.IsNullOrEmpty(BuildMetaData))
        {
            text = text + "+" + BuildMetaData;
        }

        return text;
    }

    private static void ValidateTextPart(string? text)
    {
        if (!string.IsNullOrEmpty(text) && !IdentifierRegex().IsMatch(text))
        {
            throw new Exception($"Invalid identifier: {text}. Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-].");
        }        
    }
}
