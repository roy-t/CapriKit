using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Xml;

namespace CapriKit.CommandLine;

public static class Utilities
{
    public static string ToLiteral(string text)
    {
        return SymbolDisplay.FormatLiteral(text, true);
    }

    public static string GetDocumentationFromLeadingTrivia(SyntaxNode syntax)
    {        
        if (syntax.HasLeadingTrivia)
        {
            var trivia = syntax.GetLeadingTrivia();
            foreach (var triviaNode in trivia)
            {
                var kind = triviaNode.Kind();
                // BUG: https://github.com/dotnet/roslyn/issues/58210
                // this only works if <GenerateDocumentationFile>true</> was set in the csproj file
                if (kind == SyntaxKind.SingleLineDocumentationCommentTrivia)
                {
                    var comment = triviaNode.ToFullString();
                    return GetDocumentationFromXmlComment(comment);
                }
            }
        }

        return string.Empty;
    }

    public static string GetDocumentationFromXmlComment(string comment)
    {
        try
        {
            var lines = comment.Split(['\r', '\n']).Select(c => c.Trim());
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
                    return reader.ReadInnerXml().Trim();
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        return string.Empty;
    }

    // from: https://andrewlock.net/creating-a-source-generator-part-5-finding-a-type-declarations-namespace-and-type-hierarchy/
    // determine the namespace the class/enum/struct is declared in, if any
    public static string GetNamespace(BaseTypeDeclarationSyntax syntax)
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
