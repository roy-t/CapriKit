namespace CapriKit.HLSL.TypeGenerator.Parsers;


public enum TokenKind : byte
{
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

    // Multi character operators
    Increment,
    Decrement,
    EqualsEquals,
    NotEquals,
    LessThanEquals,
    GreaterThanEquals,
    LeftShift,
    RightShift,
    LogicalAnd,
    LogicalOr,
    AddAssign,
    SubtractAssign,
    MultiplyAssign,
    DivideAssign,
    ModulusAssign,
    BitwiseAndAssign,
    BitwiseOrAssign,
    BitwiseXorAssign,
    ScopeResolution,
    PointerMemberAccess,
    LeftShiftAssign,
    RightShiftAssign,

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
    public string Value => Source.Substring(Offset, Length);

    public override string ToString()
    {
        return $"{Kind}: \"{Value}\"";
    }
}

// Every rule returns how many characters it read from the source
public delegate int Rule(string source, int cursor, List<Token> tokens);

public sealed class Tokenizer
{
    public IReadOnlyList<Token> Parse(string source)
    {
        var rules = new Rule[]
        {
            CommentTokenizer.ReadBlockComment,
            CommentTokenizer.ReadLineComment,
            CommentTokenizer.ReadDirective,
            ReservedTokenizer.ReadKeyword,
            ReservedTokenizer.ReadPunctuation,
            ReservedTokenizer.ReadOperators,
            NumberTokenizer.ReadNumber,
            IdentifierTokenizer.ReadIdentity,
        };

        var tokens = new List<Token>();

        var cursor = 0;
        while (cursor < source.Length)
        {
            cursor += ReadWhitespace(source, cursor);
            var matched = false;

            foreach (var rule in rules)
            {
                var read = rule(source, cursor, tokens);
                if (read > 0)
                {
                    cursor += read;
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                cursor += ReadUnknownToken(source, cursor, tokens);
            }
        }

        return tokens;
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

    private static int ReadUnknownToken(string source, int offset, List<Token> tokens)
    {
        var advanced = 0;
        var cursor = offset + advanced;
        while (cursor < source.Length && !char.IsWhiteSpace(source[cursor]))
        {
            advanced++;
            cursor = offset + advanced;
        }

        tokens.Add(new Token(source, offset, advanced, TokenKind.LogicalOr));
        return advanced;
    }
}
