namespace CapriKit.Meta;

public static class Utilities
{
    public static IEnumerable<string> SearchFileUp(string searchPattern, string? startingDirectory = null)
    {
        DirectoryInfo? directory = new(startingDirectory ?? Environment.CurrentDirectory);

        while (directory != null)
        {
            var files = directory.EnumerateFiles(searchPattern);
            if (files.Any())
            {
                return files.Select(f => f.FullName);
            }

            directory = directory.Parent;
        }

        return [];
    }

    public static IEnumerable<string> SearchDirectoryUp(string searchPattern, string? startingDirectory = null)
    {
        DirectoryInfo? directory = new(startingDirectory ?? Environment.CurrentDirectory);

        while (directory != null)
        {
            var files = directory.EnumerateDirectories(searchPattern);
            if (files.Any())
            {
                return files.Select(f => f.FullName);
            }

            directory = directory.Parent;
        }

        return [];
    }
}
