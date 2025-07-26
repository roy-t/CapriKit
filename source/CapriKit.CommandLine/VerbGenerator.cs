using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CapriKit.CommandLine;

internal sealed class VerbClass
{
    public VerbClass(string name, string type, string documentation, string @namespace)
    {
        Name = name;
        Type = type;
        Documentation = documentation;
        Namespace = @namespace;
    }

    public string Name { get; }
    public string Type { get; }
    public string Documentation { get; }
    public string Namespace { get; }
}

internal sealed class FlagProperty
{
    public FlagProperty(string type, string fieldName, string flagName, string documentation, string parentClass)
    {
        Type = type;
        FieldName = fieldName;
        FlagName = flagName;        
        Documentation = documentation;
        ParentClass = parentClass;        
    }

    public string Type { get; }
    public string FieldName { get; }
    public string FlagName { get; }   
    public string Documentation { get; }
    public string ParentClass { get; }
    
}

[Generator]
public class VerbGenerator : IIncrementalGenerator
{
    private static string VerbAttributeFullName = "CapriKit.CommandLine.Types.VerbAttribute";
    private static string VerbAttributeName = "VerbAttribute";

    private static string FlagAttributeFullName = "CapriKit.CommandLine.Types.FlagAttribute";
    private static string FlagAttributeName = "FlagAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var verbDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            VerbAttributeFullName,
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, ct) => GetSemanticTargetForVerbGeneration(ctx, ct))
            .Collect();

        var flagDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            FlagAttributeFullName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (ctx, ct) => GetSemanticTargetForFlagGeneration(ctx, ct))
            .Collect();

        var verbsAndFlags = verbDeclarations.Combine(flagDeclarations);
        context.RegisterSourceOutput(verbsAndFlags, static (spc, source) => Execute(spc, source));
    }

    private static void Execute(SourceProductionContext context, (ImmutableArray<(ClassDeclarationSyntax? Syntax, AttributeData? Data, string? Namespace)> VerbDeclarations, ImmutableArray<(PropertyDeclarationSyntax? Syntax, AttributeData? Data, string? ParentClass)> FlagDeclarations) source)
    {
        var flags = new List<FlagProperty>();
        foreach (var flagDeclaration in source.FlagDeclarations)
        {
            var flag = GetFlag(flagDeclaration.Syntax, flagDeclaration.Data, flagDeclaration.ParentClass);
            if (flag != null)
            {
                flags.Add(flag);
            }
        }

        var verbs = new List<VerbClass>();
        foreach(var verbDeclaration in source.VerbDeclarations)
        {
            var verb = GetVerb(verbDeclaration.Syntax, verbDeclaration.Data, verbDeclaration.Namespace);
            if (verb != null)
            {
                verbs.Add(verb);
            }
        }

        GenerateCode(context, verbs, flags);
    }

    private static void GenerateCode(SourceProductionContext context, IReadOnlyList<VerbClass> verbs, IReadOnlyList<FlagProperty> flags)
    {        
        foreach (var verb in verbs)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"namespace {verb.Namespace};");
            builder.AppendLine($"public partial class {verb.Type}");
            builder.AppendLine("{");

            foreach (var flag in flags)
            {
                if (flag.ParentClass != verb.Type)
                {
                    continue;
                }

                builder.AppendLine($"  private {flag.Type} _{flag.FieldName} = default;");
                builder.AppendLine($"  public partial {flag.Type} {flag.FieldName} {{ get => _{flag.FieldName}; }}");
            }
            // TODO this generates the partial classes but not the parsing stuff!

            builder.AppendLine("}");
            context.AddSource($"VerbGenerator.{verb.Namespace}.{verb.Type}.g.cs", builder.ToString());
        }               
    }

    private static VerbClass? GetVerb(ClassDeclarationSyntax? syntax, AttributeData? data, string? @namespace)
    {
        if (syntax == null || data == null || @namespace == null)
        {
            return null;
        }

        if (data.ConstructorArguments.Length != 1)
        {
            throw new Exception($"{VerbAttributeFullName} should have exactly one constructor argument");
        }

        var name = data.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        var type = syntax.Identifier.ValueText ?? string.Empty;        
        var documentation = GetDocumentationFromLeadingTrivia(syntax);

        return new VerbClass(name, type, documentation, @namespace);
    }

    private static FlagProperty? GetFlag(PropertyDeclarationSyntax? syntax, AttributeData? data, string? parentClass)
    {
        if (syntax == null || data == null || parentClass == null)
        {
            return null;
        }

        if (syntax.Type is not PredefinedTypeSyntax predefinedType)
        {
            return null;
        }

        if (data.ConstructorArguments.Length != 1)
        {
            throw new Exception($"{FlagAttributeFullName} should have exactly one constructor argument");
        }        

        var name = data.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        var fieldName = syntax.Identifier.ValueText ?? string.Empty;
        var type = predefinedType.Keyword.ValueText;
        var documentation = GetDocumentationFromLeadingTrivia(syntax);       

        return new FlagProperty(type, fieldName, name, documentation, parentClass);
    }

    private static string GetDocumentationFromLeadingTrivia(SyntaxNode syntax)
    {
        if(syntax.HasLeadingTrivia)
        {
            var trivia = syntax.GetLeadingTrivia();
            foreach(var triviaNode in trivia)
            {
                var kind = triviaNode.Kind();
                if (kind == SyntaxKind.SingleLineDocumentationCommentTrivia)
                {
                    var comment = triviaNode.ToFullString();
                    return GetDocumentationFromXmlComment(comment);
                }                
            }
        }

        return string.Empty;
    }

    private static string GetDocumentationFromXmlComment(string comment)
    {
        try
        {
            var lines = comment.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.StartsWith("///"))
                {
                    if (line.Length > 3)
                    {
                        builder.AppendLine(line.Substring(3).Trim());
                    }
                    else
                    {
                        builder.AppendLine();
                    }
                }
            }

            var xml = builder.ToString();

            var settings = new XmlReaderSettings()
            {
                ConformanceLevel = ConformanceLevel.Fragment
            };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "summary")
                {
                    return reader.ReadInnerXml();
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }
        
        return string.Empty;
    }    

    private static (ClassDeclarationSyntax? Syntax, AttributeData? Data, string? Namespace) GetSemanticTargetForVerbGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is ClassDeclarationSyntax classDeclaration)
        {
            var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == VerbAttributeName);
            var ns = GetNamespace(classDeclaration);
            return (classDeclaration, attribute, ns);
        }

        return (null, null, null);
    }

    private static (PropertyDeclarationSyntax? Syntax, AttributeData? Data, string? ParentClass) GetSemanticTargetForFlagGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is PropertyDeclarationSyntax properyDeclarationSyntax)
        {            

            var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == FlagAttributeName);
            var parentClass = context.TargetNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()
                ?.Identifier.ValueText ?? string.Empty;            
            return (properyDeclarationSyntax, attribute, parentClass);
        }

        return (null, null, null);
    }

    // from: https://andrewlock.net/creating-a-source-generator-part-5-finding-a-type-declarations-namespace-and-type-hierarchy/
    // determine the namespace the class/enum/struct is declared in, if any
    static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        // If we don't have a namespace at all we'll return an empty string
        // This accounts for the "default namespace" case
        string nameSpace = string.Empty;

        // Get the containing syntax node for the type declaration
        // (could be a nested type, for example)
        SyntaxNode? potentialNamespaceParent = syntax.Parent;

        // Keep moving "out" of nested classes etc until we get to a namespace
        // or until we run out of parents
        while (potentialNamespaceParent != null &&
                potentialNamespaceParent is not NamespaceDeclarationSyntax
                && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
        {
            potentialNamespaceParent = potentialNamespaceParent.Parent;
        }

        // Build up the final namespace by looping until we no longer have a namespace declaration
        if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
        {
            // We have a namespace. Use that as the type
            nameSpace = namespaceParent.Name.ToString();

            // Keep moving "out" of the namespace declarations until we 
            // run out of nested namespace declarations
            while (true)
            {
                if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                // Add the outer namespace as a prefix to the final namespace
                nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                namespaceParent = parent;
            }
        }

        // return the final namespace
        return nameSpace;
    }
}
