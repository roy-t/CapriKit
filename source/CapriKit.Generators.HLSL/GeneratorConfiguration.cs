using System.Runtime.Serialization;

namespace CapriKit.Generators.HLSL;

[DataContract]
internal sealed record GeneratorConfiguration
{
    internal GeneratorConfiguration()
    {
        TargetNamespace = string.Empty;
        ContentRoot = string.Empty;
        AbsoluteContentRoot = string.Empty;
    }

    public GeneratorConfiguration(string targetNamespace, string contentRoot, string absoluteContentRoot)
    {
        TargetNamespace = targetNamespace;
        ContentRoot = contentRoot;
        AbsoluteContentRoot = absoluteContentRoot;
    }

    [DataMember(Name = "targetNamespace", IsRequired = true)]
    public string TargetNamespace { get; set; }

    [DataMember(Name = "contentRoot", IsRequired = true)]
    public string ContentRoot { get; set; }

    public string AbsoluteContentRoot { get; set; }
}
