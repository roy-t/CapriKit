using CapriKit.Generators.PrecisionVariants.RewriteRules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

internal sealed class MathRewriteRule : ITypeRewriterRule
{
    private readonly ITypeSymbol MathFTypeSymbol;
    private readonly IdentifierNameSyntax MathFSyntax;


    public MathRewriteRule(Compilation compilation)
    {
        MathFTypeSymbol = compilation.GetTypeByMetadataName("System.MathF") ?? throw new Exception("Cannot find System.MathF");
        MathFSyntax = SyntaxFactory.IdentifierName("MathF");
    }

    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol)
    {        
        if (syntax is not QualifiedNameSyntax qualifiedNameSyntax ||
            qualifiedNameSyntax.Right is not IdentifierNameSyntax identifierNameSyntax ||
            identifierNameSyntax.Identifier.ValueText != "Math")
        {
            return (syntax, symbol);
        }
        
        syntax = qualifiedNameSyntax.WithRight(MathFSyntax);
        return (syntax, MathFTypeSymbol);
    }
}
