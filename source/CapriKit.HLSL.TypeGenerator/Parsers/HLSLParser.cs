namespace CapriKit.HLSL.TypeGenerator.Parsers;


public enum TokenKind : byte
{
    Unknown,

    Keyword,
    Comment,
    Directive,
    Reserved,
    Whitespace,
    FloatingPointLiteral,
    IntegerLiteral,
    Character,
    String,
    Identifier,
    Operator

    //// Keywords
    //Struct,
    //CBuffer,

    //// Brackets
    //LeftCurlyBracket,
    //RightCurlyBracket,
    //LeftSquareBracket,
    //RightSquareBracket,
    //LeftAngleBracket,
    //RightAngleBracket,
    //LeftParenthesis,
    //RightParenthesis,

    //// Separators
    //Comma,
    //Colon,
    //SemiColon,

    //// Operators
    //Divide,
    //Equals,
    //Minus,
    //Multiply,
    //Plus,

    //// Multi character operators
    //AddAssign,
    //AndAssign,
    //Arrow,
    //Decrement,
    //DivideAssign,
    //Ellipsis,
    //EqualsEquals,
    //GreaterThanEquals,
    //Increment,
    //LeftShift,
    //LeftShiftAssign,
    //LessThanEquals,
    //LogicalAnd,
    //LogicalOr,
    //ModulusAssign,
    //MultiplyAssign,
    //NotEquals,
    //OrAssign,
    //RightShift,
    //RightShiftAssign,
    //ScopeResolution,
    //StringizeWithAt,
    //SubtractAssign,
    //TokenPaste,
    //XorAssign,

    //// Comments
    //LineComment,
    //BlockComment,
    //Directive,

    //// Complex
    //Identifier,
    //Number,
    //NumberSuffix,

    ////
    //NumericExpansion
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
