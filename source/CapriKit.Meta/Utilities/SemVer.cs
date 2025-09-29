using System.Text.RegularExpressions;

namespace CapriKit.Meta;

// TODO: move to separate library
// TODO: add tests
public partial class SemVer
{
    [GeneratedRegex("^(?<major>0|[1-9]\\d*)\\.(?<minor>0|[1-9]\\d*)\\.(?<patch>0|[1-9]\\d*)(?:-(?<prerelease>(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVerRegex();

    [GeneratedRegex("^[a-zA-Z0-9-]+$")]
    private static partial Regex IdentifierRegex();

    public SemVer(uint major, uint minor, uint patch, string? preRelease = null, string? buildMetaData = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;

        ValidateIdentifier(preRelease);
        PreRelease = preRelease ?? string.Empty;

        ValidateIdentifier(buildMetaData);
        BuildMetaData = buildMetaData ?? string.Empty;
    }

    public uint Major { get; }
    public uint Minor { get; }
    public uint Patch { get; }
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
        var match = SemVerRegex().Match(text);
        if (match.Success)
        {
            var majorString = match.Groups[1].Value;
            var minorString = match.Groups[2].Value;
            var patchString = match.Groups[3].Value;
            var preRelease = match.Groups[4].Value;
            var buildMetaData = match.Groups[5].Value;

            if (!uint.TryParse(majorString, out var major))
            {
                throw new Exception($"The major segment in version {text} should be an unsigned integer, but was: {major}");
            }

            if (!uint.TryParse(minorString, out var minor))
            {
                throw new Exception($"The minor segment in version {text} should be an unsigned integer, but was: {minor}");
            }

            if (!uint.TryParse(patchString, out var patch))
            {
                throw new Exception($"The patch segment in version {text} should be an unsigned integer, but was: {patch}");
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

    private static void ValidateIdentifier(string? text)
    {
        if (!string.IsNullOrEmpty(text) && !IdentifierRegex().IsMatch(text))
        {
            throw new Exception($"Invalid identifier: {text}. Identifiers MUST comprise only ASCII alphanumerics and hyphens [0-9A-Za-z-].");
        }
    }
}
