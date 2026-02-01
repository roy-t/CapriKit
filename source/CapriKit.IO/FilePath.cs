namespace CapriKit.IO;

public record FilePath(DirectoryPath Directory, FileName File)
{
    public static FilePath FromSpan(ReadOnlySpan<char> path)
    {
        var file = Path.GetFileName(path);
        var directory = Path.GetDirectoryName(path);

        return new FilePath(new DirectoryPath(directory), new FileName(file));
    }

    public FilePath ToAbsolute()
    {
        return this with { Directory = this.Directory.ToAbsolute() };
    }

    public FilePath ToAbsolute(string basePath)
    {
        return this with { Directory = this.Directory.ToAbsolute(basePath) };
    }

    public override string ToString()
    {
        return $"{Directory.Path}{File.Name}";
    }
}
