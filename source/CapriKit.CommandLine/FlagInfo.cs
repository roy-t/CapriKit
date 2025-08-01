namespace CapriKit.CommandLine;

public sealed class FlagInfo
{
    public FlagInfo(string name, string type, string documentation)
    {
        Name = name;
        Type = type;
        Documentation = documentation;
    }

    public string Name { get; }
    public string Type { get; }
    public string Documentation { get; }
}
