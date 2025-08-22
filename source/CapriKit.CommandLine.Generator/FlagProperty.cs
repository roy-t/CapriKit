namespace CapriKit.CommandLine;

internal sealed class FlagProperty
{
    public FlagProperty(string propertyType, string propertyName, string parentTypeName, string flagName, string documentation)
    {
        PropertyType = propertyType;
        PropertyName = propertyName;
        ParentTypeName = parentTypeName;
        FlagName = flagName;
        Documentation = documentation;
    }

    public string PropertyType { get; }
    public string PropertyName { get; }
    public string ParentTypeName { get; }
    public string FlagName { get; }
    public string Documentation { get; }
}
