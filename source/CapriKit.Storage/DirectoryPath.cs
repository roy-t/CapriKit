namespace CapriKit.Storage;

public record DirectoryPath
{
    public DirectoryPath(string path)
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

    public static bool IsValidDirectoryPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var invalidChars = System.IO.Path.GetInvalidPathChars();
        return path.IndexOfAny(invalidChars) < 0;
    }

    public static string NormalizePathSeparators(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return path.Replace('\\', '/');
    }

    public static string AddTrailingDirectorySeparator(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || System.IO.Path.EndsInDirectorySeparator(path))
        {
            return path;
        }

        return path + '/';
    }
}
