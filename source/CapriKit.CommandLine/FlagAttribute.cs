namespace CapriKit.CommandLine;

[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public sealed class FlagAttribute : Attribute
{
    public static readonly string FullName = "CapriKit.CommandLine.Types.FlagAttribute";

    public FlagAttribute(string name)
    {
        this.Name = name;
    }

    public string Name { get; }
}
