using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class FloatingPointNumberTokenizer
{
    private readonly record struct State(int Cursor, bool HasDecimalPoint, bool HasExponent, bool HasDigits, bool HasSuffix);

    /// <summary>
    /// Parse floating point numbers.
    /// </summary>
    /// <remarks>Always try to parse a character sequence as a floating point number before trying to parse it as an integer. If not the sequence "1.0" will be parsed as 'Int 1' and 'Float .0' </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#floating-point-numbers"/>    
    public static int ReadFloatingPointNumber(string source, int offset, List<Token> tokens)
    {
        var read = ReadFloatingPointNumberFractional(source, offset, tokens);
        if (read > 0) { return read; }

        return ReadFloatingPointNumberDigitSequence(source, offset, tokens);
    }

    private static int ReadFloatingPointNumberFractional(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false, false, false);
        state = ReadFractionalConstant(source, state);
        if (state.Cursor == offset) { return 0; }

        state = ReadExponent(source, state);
        state = ReadSuffix(source, state);

        return CreateTokens(source, offset, tokens, state);
    }

    private static int ReadFloatingPointNumberDigitSequence(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false, false, false);
        state = ReadDigitSequence(source, state);
        if (state.Cursor == offset) { return 0; }

        state = ReadExponent(source, state);
        if (state.Cursor == offset) { return 0; }

        state = ReadSuffix(source, state);
        return CreateTokens(source, offset, tokens, state);
    }

    private static int CreateTokens(string source, int offset, List<Token> tokens, State state)
    {
        var length = state.Cursor - offset;
        tokens.Add(new Token(source, offset, length, TokenKind.FloatingPointLiteral));
        return length;
    }

    private static State ReadFractionalConstant(string source, State state)
    {
        // The integer part is optional        
        var cursor = state.Cursor;
        var hasDigit = false;
        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            hasDigit = true;
            cursor++;
        }

        var hasDecimalPoint = false;
        if (TryPeek(source, cursor, out var dot) && dot == '.')
        {
            hasDecimalPoint = true;
            cursor++;
        }

        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            hasDigit = true;
            cursor++;
        }

        if (hasDecimalPoint && hasDigit)
        {
            return state with
            {
                Cursor = cursor,
                HasDigits = true,
                HasDecimalPoint = true,
            };
        }

        return state;
    }

    private static State ReadDigitSequence(string source, State state)
    {
        var cursor = state.Cursor;
        var hasDigit = false;
        if (TryPeek(source, cursor, out var s) && (s == '-' || s == '+'))
        {
            cursor++;
        }
        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            hasDigit = true;
            cursor++;
        }

        if (hasDigit)
        {
            return state with
            {
                Cursor = cursor,
                HasDigits = true,
                HasDecimalPoint = true,
            };
        }

        return state;
    }

    private static State ReadExponent(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var e) && (e == 'e' || e == 'E'))
        {
            cursor++;
            cursor = SkipSign(source, cursor);

            var hasDigits = false;
            while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
            {
                hasDigits = true;
                cursor++;
            }

            if (hasDigits)
            {
                return state with
                {
                    Cursor = cursor,
                    HasExponent = true,
                };
            }
        }

        return state;
    }

    private static State ReadSuffix(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var s) && s is 'h' or 'H' or 'f' or 'F' or 'l' or 'L')
        {
            cursor++;
            return state with
            {
                Cursor = cursor,
                HasSuffix = true
            };
        }

        return state;
    }

    private static int SkipSign(string source, int cursor)
    {
        if (TryPeek(source, cursor, out var s) && (s == '-' || s == '+'))
        {
            cursor++;
        }

        return cursor;
    }
}
