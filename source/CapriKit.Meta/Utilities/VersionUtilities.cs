namespace CapriKit.Meta.Utilities;

internal static class VersionUtilities
{
    
    public static SemVer? ReadVersionFromFile()
    {
        var path = Config.VersionPath;
        if (File.Exists(path))
        {
            var text = File.ReadAllText(path).Trim();
            return SemVer.Parse(text);
        }

        return null;
    }

    public static void WriteVersionToFile(SemVer version)
    {
        var path = Config.VersionPath;
        File.WriteAllText(path, version.ToString());
    }
}
