using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal sealed class ParseState
{
    private readonly IReadOnlyList<Token> Tokens;
    private int cursor;

    public ParseState(IReadOnlyList<Token> tokens)
    {
        Tokens = [.. tokens.Where(t => !HLSLTokenizer.IsTrivia(t))];
        cursor = 0;
    }

    public bool IsAtEnd => cursor >= Tokens.Count;

    public Token Peek() => Tokens[cursor];

    public bool Peek(TokenKind kind) => !IsAtEnd && Tokens[cursor].Kind == kind;

    public bool Peek(TokenKind kind, string value) => !IsAtEnd && Tokens[cursor].Kind == kind && Tokens[cursor].Value.Equals(value);

    public bool Peek(TokenKind kind, HashSet<string> values) => !IsAtEnd && Tokens[cursor].Kind == kind && values.Contains(Tokens[cursor].Value);

    public Token Advance() => Tokens[cursor++];

    /// <summary>
    /// Captures the current cursor so a speculative parse can be rewound with <see cref="Restore"/>.
    /// </summary>
    public int Mark() => cursor;

    /// <summary>
    /// Rewinds the cursor to a position previously returned by <see cref="Mark"/>.
    /// </summary>
    public void Restore(int mark) => cursor = mark;

    public bool Match(TokenKind kind, string value)
    {
        if (Peek(kind, value))
        {
            Advance();
            return true;
        }
        return false;
    }

    public bool Match(TokenKind kind, HashSet<string> values)
    {
        if (Peek(kind, values))
        {
            Advance();
            return true;
        }
        return false;
    }

    public void ExpectKeyword(string value)
    {
        var t = Advance();
        if (t.Kind != TokenKind.Keyword || t.Value != value)
        {
            throw new Exception($"Expected keyword '{value}' but got {t.Kind} '{t.Value}'.");
        }
    }

    public string ExpectIdentifier()
    {
        var t = Advance();
        if (t.Kind != TokenKind.Identifier)
        {
            throw new Exception($"Expected identifier but got {t.Kind} '{t.Value}'.");
        }
        return t.Value;
    }

    public string ExpectType()
    {
        var t = Advance();
        if (t.Kind != TokenKind.Keyword && t.Kind != TokenKind.Identifier)
        {
            throw new Exception($"Expected type but got {t.Kind} '{t.Value}'.");
        }
        return t.Value;
    }

    public bool PeekType()
    {
        var t = Peek();
        if (t.Kind == TokenKind.Keyword || t.Kind == TokenKind.Identifier)
        {
            return true;
        }

        return false;
    }

    public void ExpectOperator(string value)
    {
        var t = Advance();
        if (t.Kind != TokenKind.Operator || t.Value != value)
        {
            throw new Exception($"Expected operator '{value}' but got {t.Kind} '{t.Value}'.");
        }
    }
}
