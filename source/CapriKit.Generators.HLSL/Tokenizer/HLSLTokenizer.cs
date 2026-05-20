namespace CapriKit.Generators.HLSL.Tokenizer;

internal enum TokenKind : byte
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
    Operator,
}

internal readonly record struct Token(string Source, int Offset, int Length, TokenKind Kind)
{
    public string Value => Source.Substring(Offset, Length);

    public override string ToString()
    {
        return $"{Kind}: \"{Value}\"";
    }
}

// Every rule returns how many characters it read from the source
internal delegate int Rule(string source, int cursor, List<Token> tokens);

internal static class HLSLTokenizer
{
    public static bool IsTrivia(Token token)
    {
        return token.Kind is TokenKind.Comment or TokenKind.Whitespace;
    }

    public static IReadOnlyList<Token> Parse(string source)
    {
        var tokens = new List<Token>();

        var rules = new Rule[]
        {
            DirectiveTokenizer.ReadDirective,

            // Grammar tokenizers
            WhitespaceTokenizer.ReadWhitespace,
            CommentTokenizer.ReadComment,
            // needs to happen before integers to prevent parsing 1.0 as integer 1 + float .0
            FloatingPointNumberTokenizer.ReadFloatingPointNumber,
            IntegerTokenizer.ReadInteger,
            CharacterTokenizer.ReadCharacter,
            StringTokenizer.ReadString,
            // Includes keywords and reserved words, that are just 'special' identifiers
            IdentifierTokenizer.ReadIdentifier,
            OperatorTokenizer.ReadOperator
        };

        var cursor = 0;
        while (cursor < source.Length)
        {
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
                throw new Exception("Tokenizer could not make progress");
            }
        }

        return tokens;
    }
}
