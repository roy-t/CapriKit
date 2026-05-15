using System.Runtime.Serialization;

namespace CapriKit.Generators.HLSL;

[DataContract]
public sealed class GeneratorConfiguration
{
    internal GeneratorConfiguration()
    {
        TargetNamespace = string.Empty;
        ContentRoot = string.Empty;
    }

    public GeneratorConfiguration(string targetNamespace, string contentRoot)
    {
        TargetNamespace = targetNamespace;
        ContentRoot = contentRoot;
    }

    [DataMember(Name = "targetNamespace", IsRequired = true)]
    public string TargetNamespace { get; set; }

    [DataMember(Name = "contentRoot", IsRequired = true)]
    public string ContentRoot { get; set; }
}
