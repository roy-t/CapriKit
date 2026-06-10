using CapriKit.Generators.HLSL.Tokenizer;
using Microsoft.CodeAnalysis.CSharp;

namespace CapriKit.Generators.HLSL.Parser.Infrastructure;

internal static class ParserBuilderUtilities
{
    private static readonly HashSet<string> InterpolationModifiers =
    [
       "linear", "centroid", "nointerpolation", "noperspective", "sample",
    ];

    private static readonly HashSet<string> StorageClasses =
    [
    "extern", "nointerpolation", "precise", "shared", "groupshared",
        "static", "uniform", "volatile",
    ];

    private static readonly HashSet<string> TypeModifiers =
    [
        "const", "row_major", "column_major"
    ];

    private static readonly HashSet<string> InputModifiers =
    [
        "in", "inout", "out", "uniform"
    ];


    public static readonly Matcher AnyType = t => t.Kind == TokenKind.Keyword || t.Kind == TokenKind.Identifier;
    public static readonly Matcher AnyIdentifier = t => t.Kind == TokenKind.Identifier;
    public static readonly Matcher AnyIntegerLiteral = t => t.Kind == TokenKind.IntegerLiteral;
    public static readonly Matcher AnyModifier = t => IsModifier(t);

    public static Matcher Keyword(string value) => t => t.Kind == TokenKind.Keyword && value.Equals(t.Value, StringComparison.Ordinal);
    public static Matcher Operator(string value) => t => t.Kind == TokenKind.Operator && value.Equals(t.Value, StringComparison.Ordinal);

    public static uint ParseRegister(string value)
    {
        // skip the letter in front of the register number
        var digits = value.Substring(1);
        return uint.Parse(digits);
    }

    /// <summary>
    /// Returns true when the token is a storage class, type or interpolation modifier.
    /// </summary>
    public static bool IsModifier(Token token) =>
        token.Kind == TokenKind.Keyword &&
        (InterpolationModifiers.Contains(token.Value) || StorageClasses.Contains(token.Value) || TypeModifiers.Contains(token.Value)) || InputModifiers.Contains(token.Value);
}
