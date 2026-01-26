using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CapriKit.Meta;

// TODO: move to separate library
// TODO: add tests
[JsonConverter(typeof(SemVerJsonConverter))]
public partial class SemVer : IComparable<SemVer>, IEquatable<SemVer>
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

    public int CompareTo(SemVer? other)
    {
        if (other is null) return 1;

        // 1. Major / Minor / Patch
        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;

        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;

        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;

        // 2. Handle prerelease
        bool thisHasPre = !string.IsNullOrEmpty(PreRelease);
        bool otherHasPre = !string.IsNullOrEmpty(other.PreRelease);

        if (!thisHasPre && !otherHasPre) return 0;
        if (!thisHasPre) return 1; // release > prerelease
        if (!otherHasPre) return -1;

        // 3. Compare prerelease identifiers
        var thisIds = PreRelease.Split('.');
        var otherIds = other.PreRelease.Split('.');

        int max = Math.Max(thisIds.Length, otherIds.Length);

        for (int i = 0; i < max; i++)
        {
            if (i >= thisIds.Length) return -1;
            if (i >= otherIds.Length) return 1;

            var a = thisIds[i];
            var b = otherIds[i];

            bool aIsNum = uint.TryParse(a, out var aNum);
            bool bIsNum = uint.TryParse(b, out var bNum);

            if (aIsNum && bIsNum)
            {
                int cmp = aNum.CompareTo(bNum);
                if (cmp != 0) return cmp;
            }
            else if (aIsNum)
            {
                return -1; // numeric < non-numeric
            }
            else if (bIsNum)
            {
                return 1;
            }
            else
            {
                int cmp = string.CompareOrdinal(a, b);
                if (cmp != 0) return cmp;
            }
        }

        return 0;
    }

    public bool Equals(SemVer? other)
    {
        if (other is null) return false;

        return Major == other.Major
            && Minor == other.Minor
            && Patch == other.Patch
            && PreRelease == other.PreRelease;
        // BuildMetaData intentionally ignored
    }

    public override bool Equals(object? obj) => Equals(obj as SemVer);

    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch, PreRelease);

    public static bool operator ==(SemVer? left, SemVer? right)
        => Equals(left, right);

    public static bool operator !=(SemVer? left, SemVer? right)
        => !Equals(left, right);

    public static bool operator <(SemVer left, SemVer right)
        => left.CompareTo(right) < 0;

    public static bool operator <=(SemVer left, SemVer right)
        => left.CompareTo(right) <= 0;

    public static bool operator >(SemVer left, SemVer right)
        => left.CompareTo(right) > 0;

    public static bool operator >=(SemVer left, SemVer right)
        => left.CompareTo(right) >= 0;
}

public class SemVerJsonConverter : JsonConverter<SemVer>
{
    public override SemVer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string token, got {reader.TokenType}");
        }
        var text = reader.GetString() ?? string.Empty;
        return SemVer.Parse(text);
    }

    public override void Write(Utf8JsonWriter writer, SemVer value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
