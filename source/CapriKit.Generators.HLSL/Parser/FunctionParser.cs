using CapriKit.Generators.HLSL.Tokenizer;
using System.Diagnostics.CodeAnalysis;
using static CapriKit.Generators.HLSL.Parser.ParserUtils;

namespace CapriKit.Generators.HLSL.Parser;

internal static class FunctionParser
{
    public static bool TryParse(ParseState state, [NotNullWhen(true)] out Function? function)
    {
        var mark = state.Mark();
        function = default;

        bool Fail()
        {
            state.Restore(mark);
            return false;
        }

        state.Match(TokenKind.Keyword, "inline");
        state.Match(TokenKind.Keyword, "precise");

        if (!state.PeekType()) { return Fail(); }
        state.Advance();

        if (!state.Peek(TokenKind.Identifier)) { return Fail(); }
        var name = state.Advance();

        if (!state.Peek(TokenKind.Operator, "(")) { return Fail(); }

        SkipArgumentList(state);
        var semantic = SemanticParser.ParseSemantic(state);
        SkipMethodBlock(state);

        function = new Function(name.Value, semantic);
        return true;
    }
}
