using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants.RewriteRules;

internal interface ITypeRewriterRule
{
    public (TypeSyntax, ITypeSymbol) Rewrite(TypeSyntax syntax, ITypeSymbol symbol);
}
