namespace CapriKit.IO;

public static class IOUtilities
{
    private const char DirectorySeperator = '/';
    private const char AltDirectorySeperator = '\\';

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    internal static StringComparison GetOSPathComparisonType()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }

    /// <summary>
    /// Changes all path separators to "/" as both the Unix and Windows APIs recognize that character
    /// </summary>
    public static ReadOnlySpan<char> NormalizePathSeparators(ReadOnlySpan<char> path)
    {
        if (!path.Contains(AltDirectorySeperator))
        {
            return path;
        }

        return path.ToString().Replace(AltDirectorySeperator, DirectorySeperator);
    }

    public static ReadOnlySpan<char> NormalizeDotSegments(ReadOnlySpan<char> path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return Path.GetFullPath(path.ToString());
        }

        throw new Exception($"Cannot normalize dot segments on relative path: {path}");
    }

    public static ReadOnlySpan<char> Normalize(ReadOnlySpan<char> path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            path = NormalizeDotSegments(path);
        }
        path = NormalizePathSeparators(path);

        return path;
    }

    public static ReadOnlySpan<char> AddTrailingDirectorySeparator(ReadOnlySpan<char> path)
    {
        if (path.Length > 0 && path[^1] != DirectorySeperator && path[^1] != AltDirectorySeperator)
        {
            return path.ToString() + DirectorySeperator;
        }

        return path;
    }

    public static ReadOnlySpan<char> RemoveTrailingDirectorySeparator(ReadOnlySpan<char> path)
    {
        if (path.Length == 0)
        {
            return path;
        }

        if (path[^1] != DirectorySeperator && path[^1] != AltDirectorySeperator)
        {
            return path;
        }

        return path[0..^1];
    }

    /// <summary>
    /// Determines if a file name contains any invalid character.
    /// </summary>
    public static bool IsValidFileName(ReadOnlySpan<char> name)
    {
        if (name.IsWhiteSpace())
        {
            return false;
        }

        return name.IndexOfAny(InvalidFileNameChars) < 0;
    }

    /// <summary>
    /// Determines if a path to a file has any invalid characters. An empty
    /// path is invalid as it does not identify a single file.
    /// </summary>    
    public static bool IsValidFilePath(ReadOnlySpan<char> path)
    {
        if (path.IsWhiteSpace())
        {
            return false;
        }

        var fileName = Path.GetFileName(path);
        var directory = Path.GetDirectoryName(path);

        return IsValidFileName(fileName) && IsValidPath(directory);
    }

    /// <summary>
    /// Determines if a path contains any invalid characters, note that an empty path is valid
    /// as it could be a relative path to the current directory
    /// </summary>
    public static bool IsValidPath(ReadOnlySpan<char> path)
    {
        return path.IndexOfAny(InvalidPathChars) < 0;
    }

    /// <summary>
    /// Looks for a file with the specified name, starting at the starting directory and then traversing up towards the root
    /// </summary>
    public static DirectoryPath? SearchForDirectoryWithMarker(DirectoryPath startingDirectory, FilePath marker)
    {
        DirectoryPath? current = startingDirectory;
        while (current != null)
        {
            var markerPath = current.Append(marker);
            if (File.Exists(markerPath))
            {
                return current;
            }

            current = current.Parent;
        }

        return null;
    }
}
