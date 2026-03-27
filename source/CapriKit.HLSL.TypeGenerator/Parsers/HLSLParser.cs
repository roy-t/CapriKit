namespace CapriKit.HLSL.TypeGenerator.Parsers;


public enum TokenKind : byte
{
    Unknown = 0,

    // Keywords
    Struct,
    CBuffer,

    // Brackets
    LeftCurlyBracket,
    RightCurlyBracket,
    LeftSquareBracket,
    RightSquareBracket,
    LeftAngleBracket,
    RightAngleBracket,
    LeftParenthesis,
    RightParenthesis,

    // Separators
    Comma,
    Colon,
    SemiColon,

    // Operators
    Equals,
    Plus,
    Minus,
    Multiply,
    Divide,

    // Comments
    LineComment,
    BlockComment,
    Directive,

    // Complex
    Identifier,
    Number,
}

public readonly record struct Token(string Source, int Offset, int Length, TokenKind Kind);


public sealed class Tokenizer
{
    private static readonly Dictionary<string, TokenKind> SimpleTokens = new()
    {
        ["struct"] = TokenKind.Struct,
        ["cbuffer"] = TokenKind.CBuffer,

        ["{"] = TokenKind.LeftCurlyBracket,
        ["}"] = TokenKind.RightCurlyBracket,
        ["["] = TokenKind.LeftSquareBracket,
        ["]"] = TokenKind.RightSquareBracket,
        ["<"] = TokenKind.LeftAngleBracket,
        [">"] = TokenKind.RightAngleBracket,
        ["("] = TokenKind.LeftParenthesis,
        [")"] = TokenKind.RightParenthesis,

        [","] = TokenKind.Comma,
        [":"] = TokenKind.Colon,
        [";"] = TokenKind.SemiColon,

        ["="] = TokenKind.Equals,
        ["+"] = TokenKind.Plus,
        ["-"] = TokenKind.Minus,
        ["*"] = TokenKind.Multiply,
        ["/"] = TokenKind.Divide,
    };

    public IReadOnlyList<Token> Parse(string source)
    {
        var tokens = new List<Token>();

        var i = 0;
        while (i < source.Length)
        {
            var start = i;
            i = ReadWhitespace(source, i);
            i = ReadSimpleToken(source, i, tokens);

            // TODO: comments (x2), directives, identifiers, numbers

            // Ensure 'i' advances even if we encounter something unexpected
            if (i == start)
            {
                i = ReadUnknownToken(source, i, tokens);
            }
        }

        return tokens;
    }

    private static int ReadWhitespace(string source, int offset)
    {
        var i = offset;
        while (i < source.Length && char.IsWhiteSpace(source[i]))
        {
            i++;
        }

        return i;
    }

    private static int ReadSimpleToken(string source, int offset, List<Token> tokens)
    {
        var span = source.AsSpan(offset);
        foreach (var kv in SimpleTokens)
        {
            var key = kv.Key;
            if (span.StartsWith(key, StringComparison.Ordinal))
            {
                tokens.Add(new Token(source, offset, key.Length, kv.Value));
                return key.Length;
            }
        }

        return 0;
    }
    private static int ReadUnknownToken(string source, int offset, List<Token> tokens)
    {
        var i = offset;
        while (i < source.Length && !char.IsWhiteSpace(source[i]))
        {
            i++;
        }

        tokens.Add(new Token(source, offset, i, TokenKind.Unknown));
        return i;
    }
}
