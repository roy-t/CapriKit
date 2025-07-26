using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;

namespace CapriKit.CommandLine;

internal sealed class VerbClass
{
    public VerbClass(string name, string documentation)
    {
        Name = name;
        Documentation = documentation;
    }

    public string Name { get; }
    public string Documentation { get; }
}

internal sealed class FlagProperty
{
    public FlagProperty(string name, string type, string documentation)
    {
        Name = name;
        Type = type;
        Documentation = documentation;
    }

    public string Name { get; }

    public string Type { get; }

    public string Documentation { get; }    
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
            static (ctx, ct) => GetSemanticTargetForVerbGeneration(ctx, ct));

        var flagDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            FlagAttributeFullName,
            static (node, _) => node is PropertyDeclarationSyntax,
            static (ctx, ct) => GetSemanticTargetForFlagGeneration(ctx, ct))
            .Collect();

        var verbsAndFlags = verbDeclarations.Combine(flagDeclarations);
        //context.RegisterSourceOutput(verbDeclarations, static (spc, source) => GenerateVerbClass(source.Syntax, source.Data, spc));

        context.RegisterSourceOutput(verbsAndFlags, static (spc, source) => { });
    }

    // TODO: this now succesfully parses the attribute and the attribute list but needs to generate code
    // TODO: need to parse the flag attribute, clean-up 


    private static VerbClass? GetVerb(ClassDeclarationSyntax? syntax, AttributeData? data)
    {
        if (syntax == null || data == null)
        {
            return null;
        }

        if (data.ConstructorArguments.Length != 1)
        {
            throw new Exception($"{VerbAttributeFullName} should have exactly one constructor argument");
        }

        var name = data.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
        var documentation = GetDocumentationFromLeadingTrivia(syntax);

        return new VerbClass(name, documentation);
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

    private static void GenerateVerbClass(ClassDeclarationSyntax? syntax, AttributeData? data, SourceProductionContext spc)
    {        
        var verb = GetVerb(syntax, data);

        spc.AddSource("generated.g.cs",
            $"""
            // <auto-generated/>
            using System;
            // Found verb {verb?.Name ?? string.Empty}
            /** documentation:
            {verb?.Documentation ?? string.Empty}
            **/
            """
        );
    }    

    private static (ClassDeclarationSyntax? Syntax, AttributeData? Data) GetSemanticTargetForVerbGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is ClassDeclarationSyntax classDeclaration)
        {
            var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == VerbAttributeName);
            return (classDeclaration, attribute);
        }

        return (null, null);
    }

    private static (PropertyDeclarationSyntax? Syntax, AttributeData? Data, string? ParentClass) GetSemanticTargetForFlagGeneration(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetNode is PropertyDeclarationSyntax properyDeclarationSyntax)
        {            

            var attribute = context.Attributes.FirstOrDefault(a => a.AttributeClass?.Name == FlagAttributeName);
            var parentClass = context.TargetNode.FirstAncestorOrSelf<ClassDeclarationSyntax>()
                ?.Identifier.ToString() ?? string.Empty;            
            return (properyDeclarationSyntax, attribute, parentClass);
        }

        return (null, null, null);
    }
}
