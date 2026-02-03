namespace CapriKit.IO;

public record DirectoryPath
{
    private readonly string Path;

    public DirectoryPath(ReadOnlySpan<char> path)
    {
        var validPath = IOUtilities.IsValidDirectoryPath(path)
            ? path
            : throw new ArgumentException($"Invalid directory path: {path}", nameof(path));

        Path = IOUtilities.AddTrailingDirectorySeparator(IOUtilities.NormalizePathSeparators(validPath));
    }

    public bool IsAbsolute => System.IO.Path.IsPathFullyQualified(Path);

    public DirectoryPath? Parent
    {
        get
        {
            var name = IOUtilities.RemoveTrailingDirectorySeparator(Path);
            var parent = System.IO.Path.GetDirectoryName(name);
            return parent.IsWhiteSpace()
                ? null
                : new DirectoryPath(parent);
        }
    }

    public DirectoryPath ToAbsolute()
    {
        var full = System.IO.Path.GetFullPath(Path);
        return new DirectoryPath(full);
    }

    public DirectoryPath ToAbsolute(DirectoryPath basePath)
    {
        if (IsAbsolute)
        {
            throw new Exception($"Cannot prepend base path {basePath} to absolute path {Path}");
        }

        var path = System.IO.Path.GetFullPath(Path, basePath.Path);
        return new DirectoryPath(path);
    }

    public DirectoryPath Join(params DirectoryPath[] path)
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

    public FilePath Join(FilePath file)
    {

        if (file.IsAbsolute)
        {
            throw new Exception($"Cannot append absolute path {file} to {Path}");
        }

        var path = System.IO.Path.Join(Path, file);
        return new FilePath(path);
    }

    public static implicit operator string(DirectoryPath? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        return path.Path;
    }

    public static implicit operator DirectoryPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new Exception("Cannot convert null or empty string to directory path");
        }

        return new DirectoryPath(path);
    }

    public static implicit operator DirectoryPath(ReadOnlySpan<char> path)
    {
        return new DirectoryPath(path);
    }
}
