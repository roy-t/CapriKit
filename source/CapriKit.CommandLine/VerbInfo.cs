namespace CapriKit.CommandLine;

public sealed class VerbInfo
{
    public VerbInfo(string name, string documentation)
    {
        Name = name;
        Documentation = documentation;
    }

    public string Name { get; }
    public string Documentation { get; }
}
