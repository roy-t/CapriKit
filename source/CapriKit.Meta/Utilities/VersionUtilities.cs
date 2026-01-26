namespace CapriKit.Meta.Utilities;

internal static class VersionUtilities
{
    private static string GetVersionFilePath()
    {
        // Find the path to the .git directory
        var rootDirectory = FileSearchUtilities.SearchDirectoryUp(".git", Environment.CurrentDirectory)
            .FirstOrDefault() ?? throw new Exception($"Not a git repository: {Environment.CurrentDirectory}");

        // The root directory of the repository, that contains the .git directory is one level "up"
        // this is also where the version file should be
        return Path.GetFullPath(Path.Combine(rootDirectory, "..", "version.txt"));
    }

    public static SemVer? ReadVersionFromFile()
    {
        var path = GetVersionFilePath();
        if (File.Exists(path))
        {
            var text = File.ReadAllText(path).Trim();
            return SemVer.Parse(text);
        }

        return null;
    }

    public static void WriteVersionToFile(SemVer version)
    {
        var path = GetVersionFilePath();
        File.WriteAllText(path, version.ToString());
    }
}
