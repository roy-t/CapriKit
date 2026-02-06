namespace CapriKit.IO;

/// <summary>
/// Represents a relative or absolute path to a directory (that might not exist).
/// The path can be an empty string but it cannot contain any illegal characters.
/// Paths are normalized to use the same directory separators and path traversals
/// are resolved for absolute paths. Comparison between file paths, directory paths
/// and string use OS specific comparison rules.
/// </summary>
public sealed class DirectoryPath : IEquatable<DirectoryPath>
{
    private readonly string Path;

    public DirectoryPath(ReadOnlySpan<char> path)
    {
        if (!IOUtilities.IsValidPath(path))
        {
            throw new ArgumentException($"Invalid directory path: {path}", nameof(path));
        }

        Path = IOUtilities.AddTrailingDirectorySeparator(
                IOUtilities.Normalize(path))
                .ToString();
    }

    public bool IsAbsolute => System.IO.Path.IsPathFullyQualified(Path);

    public DirectoryPath? Parent
    {
        get
        {
            var name = IOUtilities.RemoveTrailingDirectorySeparator(Path);
            var parent = System.IO.Path.GetDirectoryName(name);
            return parent.IsEmpty
                ? null
                : new DirectoryPath(parent);
        }
    }

    public DirectoryPath ToAbsolute()
    {
        if (IsAbsolute)
        {
            return this;
        }

        var full = System.IO.Path.GetFullPath(Path);
        return new DirectoryPath(full);
    }

    public DirectoryPath ToAbsolute(DirectoryPath basePath)
    {
        if (IsAbsolute)
        {
            throw new Exception($"Cannot prepend base path {basePath} to absolute path {Path}");
        }

        if (!basePath.IsAbsolute)
        {
            throw new Exception($"Cannot get the absolute path to {Path} using a relative base path ({basePath})");
        }

        var path = System.IO.Path.GetFullPath(Path, basePath.Path);
        return new DirectoryPath(path);
    }

    public DirectoryPath GetPathRelativeTo(DirectoryPath basePath)
    {
        if (StartsWith(basePath))
        {
            var relativePath = System.IO.Path.GetRelativePath(basePath, Path);
            return new DirectoryPath(relativePath);
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

    public DirectoryPath Append(DirectoryPath[] path)
    {
        var pathString = Path;
        foreach (var p in path)
        {
            if (p.IsAbsolute)
            {
                throw new Exception($"Cannot append absolute path {p.Path} to {pathString}");
            }

            pathString = System.IO.Path.Join(pathString, p.Path);
        }


        return new DirectoryPath(pathString);
    }

    public FilePath Append(FilePath file)
    {

        if (file.IsAbsolute)
        {
            throw new Exception($"Cannot append absolute path {file} to {Path}");
        }

        var path = System.IO.Path.Join(Path, file);
        return new FilePath(path);
    }

    public override string ToString()
    {
        return Path;
    }

    public static implicit operator string(DirectoryPath? path)
    {
        if (path == null)
        {
            return string.Empty;
        }

        return path.Path;
    }

    public static implicit operator DirectoryPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception($"Cannot convert null or empty string to {nameof(DirectoryPath)}.");
        }

        return new DirectoryPath(path);
    }

    public static implicit operator DirectoryPath(ReadOnlySpan<char> path)
    {
        return new DirectoryPath(path);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DirectoryPath);
    }

    public bool Equals(DirectoryPath? other)
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        return other != null && other.Path.Equals(Path, comparisonType);
    }

    public static bool operator ==(DirectoryPath? left, DirectoryPath? right) => Equals(left, right);
    public static bool operator !=(DirectoryPath? left, DirectoryPath? right) => !Equals(left, right);

    public override int GetHashCode()
    {
        var comparisonType = IOUtilities.GetOSPathComparisonType();
        return StringComparer.FromComparison(comparisonType).GetHashCode(Path);
    }
}
