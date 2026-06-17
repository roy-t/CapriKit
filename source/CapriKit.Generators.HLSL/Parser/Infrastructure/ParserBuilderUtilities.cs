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
    public static Matcher Pragma(string value) => t => TryGetPragmaName(t, out var name) && value.Equals(name, StringComparison.Ordinal);
    public static readonly Matcher AnyPragma = t => TryGetPragmaName(t, out var _);

    /// <summary>
    /// Extracts the name of a <c>#pragma</c> directive, for example <c>VertexShader</c> from <c>#pragma VertexShader</c>.
    /// </summary>
    public static bool TryGetPragmaName(Token token, out string name)
    {
        name = string.Empty;
        if (token.Kind != TokenKind.Directive)
        {
            return false;
        }

        var parts = token.Value.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts[0] == "#pragma")
        {
            name = parts[1];
            return true;
        }

        return false;
    }

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
