namespace CapriKit.IO;

public sealed class FilePath : IEquatable<FilePath>
{
    private readonly string Path;

    public FilePath(ReadOnlySpan<char> path)
    {
        if (!IOUtilities.IsValidFilePath(path))
        {
            throw new ArgumentException($"Invalid file path {path}", nameof(path));
        }

        if (System.IO.Path.IsPathFullyQualified(path))
        {
            path = IOUtilities.NormalizeDotSegments(path);
        }

        Path = IOUtilities.NormalizePathSeparators(path).ToString();
    }

    public ReadOnlySpan<char> FileName => System.IO.Path.GetFileName(Path.AsSpan());

    public ReadOnlySpan<char> FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path.AsSpan());

    public ReadOnlySpan<char> Extension => System.IO.Path.GetExtension(Path.AsSpan());

    public DirectoryPath Directory => new(System.IO.Path.GetDirectoryName(Path.AsSpan()));

    public bool IsAbsolute => System.IO.Path.IsPathFullyQualified(Path);

    public FilePath ToAbsolute() => new(System.IO.Path.GetFullPath(Path));

    public FilePath ToAbsolute(DirectoryPath basePath)
    {
        if (IsAbsolute)
        {
            throw new Exception($"Cannot prepend base path {basePath} to absolute path {Path}");
        }

        var path = System.IO.Path.GetFullPath(Path, basePath);
        return new FilePath(path);
    }

    public FilePath GetPathRelativeTo(DirectoryPath basePath)
    {
        if (StartsWith(basePath))
        {
            var relativePath = System.IO.Path.GetRelativePath(basePath, Path);
            return new FilePath(relativePath);
        }

        throw new Exception($"File {Path} is not in the given base path {basePath} or a sub directory of it.");
    }

    public bool StartsWith(string beginning)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        var normalized = IOUtilities.Normalize(beginning);
        return Path.StartsWith(normalized, comparisonType);
    }

    public bool Contains(string segment)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        var normalized = IOUtilities.Normalize(segment);
        return Path.Contains(normalized, comparisonType);
    }

    public bool EndsWith(string ending)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        var normalized = IOUtilities.Normalize(ending);
        return Path.EndsWith(normalized, comparisonType);
    }

    public override string ToString()
    {
        return Path;
    }

    public static implicit operator string(FilePath? path)
    {
        if (path == null)
        {
            return string.Empty;
        }

        return path.Path;
    }

    public static implicit operator FilePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception("Cannot convert null or empty string to file path");
        }

        return new FilePath(path);
    }

    public static implicit operator FilePath(ReadOnlySpan<char> path)
    {
        return new FilePath(path);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as FilePath);
    }

    public bool Equals(FilePath? other)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        return other != null && other.Path.Equals(Path, comparisonType);
    }


    public static bool operator ==(FilePath? left, FilePath? right) => Equals(left, right);
    public static bool operator !=(FilePath? left, FilePath? right) => !Equals(left, right);

    public override int GetHashCode()
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        return StringComparer.FromComparison(comparisonType).GetHashCode(Path);
    }
}
