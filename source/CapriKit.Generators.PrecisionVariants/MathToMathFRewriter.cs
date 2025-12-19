using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CapriKit.Generators.PrecisionVariants;

internal sealed class MathToMathFRewriter : CSharpSyntaxRewriter
{
    private readonly SemanticModel SemanticModel;
    private readonly INamedTypeSymbol SystemMath;
    private readonly INamedTypeSymbol SystemMathF;

    public MathToMathFRewriter(SemanticModel semanticModel, Compilation compilation)
    {
        SemanticModel = semanticModel;
        SystemMath = compilation.GetTypeByMetadataName("System.Math") ?? throw new Exception("Cannot find System.Math");
        SystemMathF = compilation.GetTypeByMetadataName("System.MathF") ?? throw new Exception("Cannot find System.MathF");
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        var visited = (MemberAccessExpressionSyntax)(base.VisitMemberAccessExpression(node) ?? throw new Exception("Invalid conversion"));
        if (visited.Expression is IdentifierNameSyntax)
        {
            var symbol = SemanticModel.GetSymbolInfo(visited.Expression);
            if (symbol.Symbol is INamedTypeSymbol namedTypeSymbol &&
                SymbolEqualityComparer.Default.Equals(namedTypeSymbol, SystemMath))
            {
                var qualifiedType = SystemMathF.ToDisplayString(TypeQualificationRewriter.FullyQualifiedTypeFormat);
                var expression = SyntaxFactory.ParseTypeName(qualifiedType)
                                      .WithTriviaFrom(visited.Expression);

                return visited.WithExpression(expression);
            }
        }
        return visited;
    }

    //public override SyntaxNode? Visit(SyntaxNode? node)
    //{
    //    if (node != null &&
    //        node.SyntaxTree.HasCompilationUnitRoot &&
    //        node is IdentifierNameSyntax identifierName)
    //    {
    //        var symbol = SemanticModel.GetSymbolInfo(node);
    //        if (symbol.Symbol is INamedTypeSymbol namedTypeSymbol &&
    //            SymbolEqualityComparer.Default.Equals(namedTypeSymbol, SystemMath))
    //        {
    //            var qualifiedType = SystemMathF.ToDisplayString(TypeQualificationRewriter.FullyQualifiedTypeFormat);
    //            node = SyntaxFactory.ParseTypeName(qualifiedType)
    //                                  .WithTriviaFrom(identifierName);
    //        }

    //    }
    //    return base.Visit(node);
    //}    
}
