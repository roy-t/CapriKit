using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Generators.HLSL.Parser;

internal sealed class ParseState
{
    private readonly IReadOnlyList<Token> tokens;
    private int cursor;

    public ParseState(IReadOnlyList<Token> tokens)
    {
        this.tokens = [.. tokens.Where(t => !HLSLTokenizer.IsTrivia(t))];
        cursor = 0;
    }

    public bool IsAtEnd => cursor >= tokens.Count;

    public Token Peek() => tokens[cursor];

    public bool Peek(TokenKind kind) => !IsAtEnd && tokens[cursor].Kind == kind;

    public bool Peek(TokenKind kind, string value) => !IsAtEnd && tokens[cursor].Kind == kind && tokens[cursor].Value.Equals(value);

    public bool Peek(TokenKind kind, HashSet<string> values) => !IsAtEnd && tokens[cursor].Kind == kind && values.Contains(tokens[cursor].Value);

    public Token Advance() => tokens[cursor++];

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
