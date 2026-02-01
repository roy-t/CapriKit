namespace CapriKit.IO;

public record DirectoryPath
{
    private const char AltDirectorySeperator = '\\';
    private const char DirectorySeperator = '/';
    private static readonly char[] InvalidPathChars = System.IO.Path.GetInvalidPathChars();

    public DirectoryPath(ReadOnlySpan<char> path)
    {
        var validPath = IsValidDirectoryPath(path) ? path : throw new ArgumentException($"Invalid directory path: {path}");
        Path = AddTrailingDirectorySeparator(NormalizePathSeparators(validPath));
    }

    public string Path { get; }

    public bool IsAbsolute => System.IO.Path.IsPathFullyQualified(Path);

    public DirectoryPath ToAbsolute()
    {
        var full = System.IO.Path.GetFullPath(Path);
        return new DirectoryPath(full);
    }

    public DirectoryPath ToAbsolute(DirectoryPath basePath)
    {
        var full = System.IO.Path.GetFullPath(Path, basePath.Path);
        return new DirectoryPath(full);
    }

    public DirectoryPath Join(params DirectoryPath[] path)
    {
        var pathString = Path;
        foreach (var p in path)
        {
            if (p.IsAbsolute)
            {
                throw new Exception($"Cannot append absolute path {p.Path}");
            }

            pathString = System.IO.Path.Join(pathString, p.Path);
        }


        return new DirectoryPath(pathString);
    }

    public FilePath Join(FileName file)
    {
        return new FilePath(this, file);
    }

    public static bool IsValidDirectoryPath(ReadOnlySpan<char> path)
    {
        // If this is a relative path an empty directory name is valid
        return path.IndexOfAny(InvalidPathChars) < 0;
    }

    public static ReadOnlySpan<char> NormalizePathSeparators(ReadOnlySpan<char> path)
    {
        if (path.IsWhiteSpace())
        {
            return path;
        }

        return path.ToString().Replace(AltDirectorySeperator, DirectorySeperator);
    }

    public static string AddTrailingDirectorySeparator(ReadOnlySpan<char> path)
    {
        if (path.Length > 0 && path[^1] != DirectorySeperator && path[^1] != AltDirectorySeperator)
        {
            return path.ToString() + DirectorySeperator;
        }

        return path.ToString();
    }
}
