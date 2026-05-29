using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal static class ParserUtils
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

    public static void SkipArgumentList(ParseState state)
    {
        SkipSegment(state, "(", ")");
    }

    public static void SkipMethodBlock(ParseState state)
    {
        SkipSegment(state, "{", "}");
    }

    /// <summary>
    /// Registers are 1 letter followed by an uint, like t0 for texture register 0
    /// </summary>
    public static uint ParseRegisterIndex(string identifier)
    {
        return uint.Parse(identifier.Substring(1));
    }

    /// <summary>
    /// Consumes optional storage class, type and interpolation modifiers for variables, members and parameters
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-variable-syntax#parameters"/>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct#interpolation-modifiers-introduced-in-shader-model-4"/>
    public static List<string> ConsumeModifiers(ParseState state)
    {
        var modifiers = new List<string>();
        while (!state.IsAtEnd && IsModifier(state.Peek()))
        {
            modifiers.Add(state.Peek().Value);
            state.Advance();
        }

        return modifiers;
    }

    /// <summary>
    /// Returns true when the token is a storage class, type or interpolation modifier.
    /// </summary>
    public static bool IsModifier(Token token) =>
        token.Kind == TokenKind.Keyword &&
        (InterpolationModifiers.Contains(token.Value) || StorageClasses.Contains(token.Value) || TypeModifiers.Contains(token.Value));

    private static void SkipSegment(ParseState state, string open, string close)
    {
        state.ExpectOperator(open);
        var depth = 1;
        while (!state.IsAtEnd && depth > 0)
        {
            var token = state.Advance();
            if (token.Kind == TokenKind.Operator)
            {
                if (token.Value == open)
                {
                    depth++;
                }
                else if (token.Value == close)
                {
                    depth--;
                }
            }
        }
    }
}
