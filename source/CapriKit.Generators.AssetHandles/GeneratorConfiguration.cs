using System.Runtime.Serialization;

namespace CapriKit.Generators.AssetHandles;

[DataContract]
internal sealed record GeneratorConfiguration
{
    internal GeneratorConfiguration()
    {
        TargetNamespace = string.Empty;
        ContentRoot = string.Empty;
        IncludedExtensions = [];
        AbsoluteContentRoot = string.Empty;
    }

    public GeneratorConfiguration(string targetNamespace, string contentRoot, IReadOnlyList<string> includedExtensions, string absoluteContentRoot)
    {
        TargetNamespace = targetNamespace;
        ContentRoot = contentRoot;
        IncludedExtensions = includedExtensions;
        AbsoluteContentRoot = absoluteContentRoot;
    }

    [DataMember(Name = "targetNamespace", IsRequired = true)]
    public string TargetNamespace { get; set; }

    [DataMember(Name = "contentRoot", IsRequired = true)]
    public string ContentRoot { get; set; }

    [DataMember(Name = "includedExtensions", IsRequired = true)]
    public IEnumerable<string> IncludedExtensions { get; set; }

    public string AbsoluteContentRoot { get; set; }
}
