using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Assertions.Exceptions;

namespace CapriKit.Tests.TestUtilities;

internal static class SyntaxRewriterExtensions
{
    public static string Execute(this CSharpSyntaxRewriter rewriter, string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var rewritten = rewriter.Visit(syntaxTree.GetRoot());

        if (rewritten == null)
        {
            throw new AssertionException("Rewriter.Visit() should not return an empty result");
        }

        return rewritten.NormalizeWhitespace().ToFullString();
    }
}
