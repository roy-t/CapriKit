using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;

namespace CapriKit.Generators.HLSL.Parser;

internal static class VariableParser
{
    private static readonly HashSet<string> StorageClasses =
    [
        "extern", "nointerpolation", "precise", "shared", "groupshared",
        "static", "uniform", "volatile",
    ];

    private static readonly HashSet<string> TypeModifiers =
    [
        "const", "row_major", "column_major"
    ];

    /// <summary>
    /// Parses HLSL variable declarations.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-variable-syntax"/>
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Variable? variable)
    {
        variable = default;
        var mark = state.Mark();

        // Skip optional storage class and type modifiers
        SkipModifiers(state);

        // Expect a type, which could be a keyword (such as bool) or any identifier)
        if (!state.Peek(TokenKind.Keyword) && !state.Peek(TokenKind.Identifier))
        {
            state.Restore(mark);
            return false;
        }
        var type = state.Advance().Value;

        if (!state.Peek(TokenKind.Identifier))
        {
            state.Restore(mark);
            return false;
        }
        var name = state.Advance().Value;

        if (state.Peek(TokenKind.Operator, "("))
        {
            // Function declaration, not a variable.
            state.Restore(mark);
            return false;
        }

        // We are no pretty sure this is a variable
        SkipArraySuffix(state);

        var register = 0;
        while (state.Peek(TokenKind.Operator, ":"))
        {
            state.Advance();
            if (state.Match(TokenKind.Keyword, "register"))
            {
                state.ExpectOperator("(");
                register = ParseRegisterIndex(state.ExpectIdentifier());
                state.ExpectOperator(")");
            }
            else if (state.Match(TokenKind.Keyword, "packoffset"))
            {
                state.ExpectOperator("(");
                SkipUntilCloseParen(state);
                state.ExpectOperator(")");
            }
            else
            {
                state.ExpectIdentifier();
            }
        }

        if (state.Match(TokenKind.Operator, "="))
        {
            SkipInitializer(state);
        }

        state.ExpectOperator(";");

        variable = new Variable(type, name, register);
        return true;
    }

    private static void SkipModifiers(ParseState state)
    {
        var isRelevant = true;
        while (isRelevant && !state.IsAtEnd)
        {
            isRelevant = state.Match(TokenKind.Keyword, StorageClasses) || state.Match(TokenKind.Keyword, TypeModifiers);
        }
    }

    private static void SkipArraySuffix(ParseState state)
    {
        if (!state.Match(TokenKind.Operator, "[")) return;
        if (!state.Peek(TokenKind.Operator, "]"))
        {
            state.Advance();
        }
        state.ExpectOperator("]");
    }

    // "s0" => 0, "t4" => 4
    private static int ParseRegisterIndex(string identifier)
    {
        return int.Parse(identifier.Substring(1));
    }

    private static void SkipUntilCloseParen(ParseState state)
    {
        var depth = 1;
        while (!state.IsAtEnd && depth > 0)
        {
            var token = state.Peek();
            if (token.Kind == TokenKind.Operator)
            {
                if (token.Value == "(") depth++;
                else if (token.Value == ")")
                {
                    if (--depth == 0) return;
                }
            }
            state.Advance();
        }
    }

    private static void SkipInitializer(ParseState state)
    {
        var depth = 0;
        while (!state.IsAtEnd)
        {
            var token = state.Peek();
            if (depth == 0 && token.Kind == TokenKind.Operator && token.Value == ";") return;
            if (token.Kind == TokenKind.Operator)
            {
                if (token.Value is "(" or "{" or "[") depth++;
                else if (token.Value is ")" or "}" or "]") depth--;
            }
            state.Advance();
        }
    }
}
