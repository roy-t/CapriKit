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
    NumberSuffix,
}

public readonly record struct Token(string Source, int Offset, int Length, TokenKind Kind)
{
    public override string ToString()
    {
        return $"{Kind}: \"{Source.Substring(Offset, Length)}\"";
    }
}


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

        var cursor = 0;
        while (cursor < source.Length)
        {
            // Every rule returns how many characters it read from the source
            var start = cursor;
            cursor += ReadBlockComment(source, cursor, tokens);
            cursor += ReadLineComment(source, cursor, tokens);
            cursor += ReadDirective(source, cursor, tokens);
            cursor += ReadNumber(source, cursor, tokens);

            cursor += ReadSimpleToken(source, cursor, tokens);
            cursor += ReadWhitespace(source, cursor);



            // TODO: comments (x2), directives, identifiers, numbers

            // Ensure 'i' advances even if we encounter something unexpected
            if (cursor == start)
            {
                cursor += ReadUnknownToken(source, cursor, tokens);
            }
        }

        return tokens;
    }

    private static int ReadNumber(string source, int offset, List<Token> tokens)
    {
        if (offset >= source.Length)
            return 0;

        var advanced = 0;
        var cursor = offset;

        var hasDot = false;
        var hasExponent = false;
        var hasDigit = false;

        if (char.IsDigit(source[cursor]))
        {
            hasDigit = true;
            advanced++;
            cursor = offset + advanced;
        }
        else if (source[cursor] == '.')
        {
            hasDot = true;
            advanced++;
            cursor = offset + advanced;
        }
        else
        {
            return 0;
        }

        while (cursor < source.Length)
        {
            char c = source[cursor];

            if (char.IsDigit(c))
            {
                hasDigit = true;
                advanced++;
                cursor = offset + advanced;
            }
            else if (c == '.' && !hasDot && !hasExponent)
            {
                hasDot = true;
                advanced++;
                cursor = offset + advanced;
            }
            else if ((c == 'e' || c == 'E') && !hasExponent && hasDigit)
            {
                hasExponent = true;
                advanced++;
                cursor = offset + advanced;

                if (cursor < source.Length && (source[cursor] == '+' || source[cursor] == '-'))
                {
                    advanced++;
                    cursor = offset + advanced;
                }

                while (cursor < source.Length && char.IsDigit(source[cursor]))
                {
                    advanced++;
                    cursor = offset + advanced;
                }
            }
            else
            {
                break;
            }
        }

        if (advanced > 0 && hasDigit)
        {
            tokens.Add(new Token(source, offset, advanced, TokenKind.Number));
        }

        if (cursor < source.Length && source[cursor] is 'u' or 'U' or 'l' or 'L' or 'f' or 'F' or 'h' or 'H')
        {
            tokens.Add(new Token(source, cursor, 1, TokenKind.NumberSuffix));
            advanced++;
        }

        return advanced;
    }

    private static int ReadBlockComment(string source, int offset, List<Token> tokens)
    {
        if ((offset + 1) < source.Length && source[offset] == '/' && source[offset + 1] == '*')
        {
            var advanced = 2;
            var cursor = offset + advanced;

            while (cursor < source.Length && !(source[cursor - 1] == '*' && source[cursor] == '/'))
            {
                advanced++;
                cursor = offset + advanced;
            }

            if (cursor + 1 < source.Length)
            {
                advanced++;
            }

            tokens.Add(new Token(source, offset, advanced, TokenKind.BlockComment));
            return advanced;
        }

        return 0;
    }


    private static int ReadLineComment(string source, int offset, List<Token> tokens)
    {
        if ((offset + 1) < source.Length && source[offset] == '/' && source[offset + 1] == '/')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            tokens.Add(new Token(source, offset, advanced, TokenKind.LineComment));
            return advanced;
        }

        return 0;
    }

    private static int ReadDirective(string source, int offset, List<Token> tokens)
    {
        if (source.Length < offset && source[offset] == '#')
        {
            var advanced = AdvancePastEndOfLine(source, offset);
            var token = new Token(source, offset, advanced, TokenKind.Directive);
            tokens.Add(token);

            return advanced;
        }

        return 0;
    }

    /// <summary>
    /// Consumes all characters remaining on this line. If this is not the last line in the file
    /// it will also consume the \n character itself.
    /// </summary>
    private static int AdvancePastEndOfLine(string source, int offset)
    {
        var advanced = 0;
        var cursor = offset + advanced;
        while (cursor < source.Length && source[cursor] != '\n')
        {
            advanced++;
            cursor = offset + advanced;
        }

        if (cursor + 1 < source.Length)
        {
            advanced++;
        }

        return advanced;
    }

    private static int ReadWhitespace(string source, int offset)
    {
        var advanced = 0;
        var cursor = advanced + offset;
        while (cursor < source.Length && char.IsWhiteSpace(source[cursor]))
        {
            advanced++;
            cursor = offset + advanced;
        }

        return advanced;
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
        var advanced = 0;
        var cursor = offset + advanced;
        while (cursor < source.Length && !char.IsWhiteSpace(source[cursor]))
        {
            advanced++;
            cursor = offset + advanced;
        }

        tokens.Add(new Token(source, offset, advanced, TokenKind.Unknown));
        return advanced;
    }
}
