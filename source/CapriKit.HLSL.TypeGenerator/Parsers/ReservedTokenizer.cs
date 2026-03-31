namespace CapriKit.HLSL.TypeGenerator.Parsers;

public static class ReservedTokenizer
{
    private static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        ["struct"] = TokenKind.Struct,
        ["cbuffer"] = TokenKind.CBuffer,
    };

    private static readonly Dictionary<char, TokenKind> Punctuation = new()
    {
        ['{'] = TokenKind.LeftCurlyBracket,
        ['}'] = TokenKind.RightCurlyBracket,
        ['['] = TokenKind.LeftSquareBracket,
        [']'] = TokenKind.RightSquareBracket,
        ['<'] = TokenKind.LeftAngleBracket,
        ['>'] = TokenKind.RightAngleBracket,
        ['('] = TokenKind.LeftParenthesis,
        [')'] = TokenKind.RightParenthesis,

        [','] = TokenKind.Comma,
        [':'] = TokenKind.Colon,
        [';'] = TokenKind.SemiColon,
    };

    private static readonly Dictionary<char, TokenKind> Operators = new()
    {
        ['='] = TokenKind.Equals,
        ['+'] = TokenKind.Plus,
        ['-'] = TokenKind.Minus,
        ['*'] = TokenKind.Multiply,
        ['/'] = TokenKind.Divide,
    };

    private static readonly Dictionary<string, TokenKind> MultiCharacterOperators = new()
    {
        ["++"] = TokenKind.Increment,
        ["--"] = TokenKind.Decrement,
        ["=="] = TokenKind.EqualsEquals,
        ["!="] = TokenKind.NotEquals,
        ["<="] = TokenKind.LessThanEquals,
        [">="] = TokenKind.GreaterThanEquals,
        ["&&"] = TokenKind.LogicalAnd,
        ["||"] = TokenKind.LogicalOr,
        ["<<"] = TokenKind.LeftShift,
        [">>"] = TokenKind.RightShift,
        ["+="] = TokenKind.AddAssign,
        ["-="] = TokenKind.SubtractAssign,
        ["*="] = TokenKind.MultiplyAssign,
        ["/="] = TokenKind.DivideAssign,
        ["%="] = TokenKind.ModulusAssign,
        ["&="] = TokenKind.BitwiseAndAssign,
        ["|="] = TokenKind.BitwiseOrAssign,
        ["^="] = TokenKind.BitwiseXorAssign,
        ["::"] = TokenKind.ScopeResolution,
        ["->"] = TokenKind.PointerMemberAccess,

        ["<<="] = TokenKind.LeftShiftAssign,
        [">>="] = TokenKind.RightShiftAssign,
    };

    public static int ReadKeyword(string source, int offset, List<Token> tokens)
    {
        return ReadString(source, offset, tokens, Keywords);
    }

    public static int ReadPunctuation(string source, int offset, List<Token> tokens)
    {
        return ReadCharacter(source, offset, tokens, Punctuation);
    }

    public static int ReadOperators(string source, int offset, List<Token> tokens)
    {
        var read = ReadString(source, offset, tokens, MultiCharacterOperators);
        if (read > 0)
        {
            return read;
        }

        return ReadCharacter(source, offset, tokens, Operators);
    }

    private static int ReadString(string source, int offset, List<Token> tokens, Dictionary<string, TokenKind> lookUp)
    {
        var span = source.AsSpan(offset);
        foreach (var kv in lookUp)
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

    private static int ReadCharacter(string source, int offset, List<Token> tokens, Dictionary<char, TokenKind> lookUp)
    {
        if (offset >= source.Length)
        {
            return 0;
        }

        var c = source[offset];
        if (lookUp.TryGetValue(c, out var kind))
        {
            tokens.Add(new Token(source, offset, 1, kind));
            return 1;
        }

        return 0;
    }
}
