namespace CapriKit.CommandLine;

internal sealed class VerbClass
{
    public VerbClass(string typeName, string typeNamespace, string verbName, string documentation)
    {
        TypeName = typeName;
        TypeNamespace = typeNamespace;
        VerbName = verbName;
        Documentation = documentation;
    }

    public string TypeName { get; }
    public string TypeNamespace { get; }
    public string VerbName { get; }
    public string Documentation { get; }

}
