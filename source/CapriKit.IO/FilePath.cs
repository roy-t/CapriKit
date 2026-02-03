namespace CapriKit.IO;

public record FilePath
{
    private readonly string Path;

    public FilePath(ReadOnlySpan<char> path)
    {
        var validPath = IOUtilities.IsValidFilePath(path)
            ? path
            : throw new ArgumentException($"Invalid file path {path}", nameof(path));

        Path = IOUtilities.NormalizePathSeparators(validPath).ToString();
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
}
