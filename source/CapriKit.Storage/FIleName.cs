namespace CapriKit.Storage;

public record FileName
{
    public FileName(string name)
    {
        Name = IsValidFileName(name) ? name : throw new ArgumentException($"Invalid file name: {name}");
    }

    public string Name { get; }

    public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(Name);

    public string Extension => Path.GetExtension(Name);

    public static bool IsValidFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        return name.IndexOfAny(invalidChars) < 0;
    }
}
