namespace CapriKit.HLSL.TypeGenerator.Parsers;


public enum TokenKind : byte
{
    Unknown = 0,
    Struct,
    Identifier,
    LeftCurlyBracket,
    RightCurlyBracket,
    Colon,
    SemiColon,
}

public readonly record struct Token(ReadOnlyMemory<char> Slice, TokenKind Kind)
{
    public static Token Empty = new(ReadOnlyMemory<char>.Empty, TokenKind.Unknown);
}

public interface ITokenizerRule
{
    int Consume(ReadOnlyMemory<char> segment, out Token token);
}

public class SingleCharTokenizer : ITokenizerRule
{
    private static readonly Dictionary<char, TokenKind> Rules = new()
    {
        ['{'] = TokenKind.LeftCurlyBracket,
        ['}'] = TokenKind.RightCurlyBracket,
        [':'] = TokenKind.Colon,
        [';'] = TokenKind.SemiColon,
    };

    public int Consume(ReadOnlyMemory<char> segment, out Token token)
    {
        if (segment.Length < 1)
        {
            throw new Exception("Cannot consume a token on an empty segment");
        }

        var c = segment.Span[0];
        if (Rules.TryGetValue(c, out var kind))
        {
            token = new Token(segment.Slice(0, 1), kind);
            return 1;
        }

        token = Token.Empty;
        return 0;
    }
}

public sealed class Tokenizer
{
    private readonly List<ITokenizerRule> Rules =
    [
        new SingleCharTokenizer(),
    ];

    public IReadOnlyList<Token> Parse(ReadOnlySpan<char> source)
    {
        var tokens = new List<Token>();

        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];

            foreach (var rule in Rules)
            {
                var segment = source.Slice(i);
                var consumed = rule.Consume(segment, out var token);
                if (consumed > 0)
                {
                    tokens.Add(token);
                    i += consumed;
                }
            }
        }

        return tokens;
    }
}



public sealed class HLSLParser
{
}
