namespace CapriKit.IO;

public record FilePath(DirectoryPath Path, FileName File)
{
    public override string ToString()
    {
        return $"{Path.Path}{File.Name}";
    }
}
