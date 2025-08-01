namespace CapriKit.CommandLine;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class VerbAttribute : Attribute
{
    public static readonly string FullName = "CapriKit.CommandLine.Types.VerbAttribute";

    public VerbAttribute(string name)
    {
        this.Name = name;
    }

    public string Name { get; }
}
