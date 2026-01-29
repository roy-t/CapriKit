namespace CapriKit.IO;

public record FileName
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    public FileName(ReadOnlySpan<char> name)
    {
        Name = IsValidFileName(name) ? name.ToString() : throw new ArgumentException($"Invalid file name: {name}");
    }

    public string Name { get; }

    public ReadOnlySpan<char> FileNameWithoutExtension => Path.GetFileNameWithoutExtension(Name.AsSpan());

    public ReadOnlySpan<char> Extension => Path.GetExtension(Name.AsSpan());

    public static bool IsValidFileName(ReadOnlySpan<char> name)
    {
        if (name.IsWhiteSpace())
        {
            return false;
        }

        return name.IndexOfAny(InvalidFileNameChars) < 0;
    }

    public static implicit operator string(FileName value) { return value.Name; }
}
