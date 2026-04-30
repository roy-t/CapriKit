using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class IntegerTokenizer
{
    private readonly record struct State(int Cursor, bool HasDigits, bool HasSuffix);

    /// <summary>
    /// Parse integers. Emits an extra token if the number ends with a suffix.
    /// </summary>
    /// <remarks>Always try to parse a character sequence as a floating point number before trying to parse it as an integer. If not the sequence "1.0" will be parsed as 'Int 1' and 'Float .0' </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#integer-numbers" />    
    public static int ReadInteger(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false);
        state = ReadIntegerConstant(source, state);
        if (state.Cursor > offset)
        {
            state = ReadSuffix(source, state);
            var length = state.Cursor - offset;

            tokens.Add(new Token(source, offset, length, TokenKind.IntegerLiteral));

            return length;
        }

        return 0;
    }

    private static State ReadIntegerConstant(string source, State state)
    {
        var cursor = state.Cursor;
        state = ReadHex(source, state);
        if (state.Cursor > cursor) { return state; }
        state = ReadOctal(source, state);
        if (state.Cursor > cursor) { return state; }
        return ReadDecimal(source, state);
    }

    private static State ReadOctal(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var o) && o == '0')
        {
            cursor++;
            var hasDigits = false;
            while (TryPeek(source, cursor, out var d) && d >= '0' && d <= '7')
            {
                hasDigits = true;
                cursor++;
            }

            if (hasDigits)
            {
                return state with
                {
                    Cursor = cursor,
                    HasDigits = true
                };
            }
        }

        return state;
    }

    private static State ReadHex(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var o) && o == '0')
        {
            cursor++;
            if (TryPeek(source, cursor, out var x) && x == 'x')
            {
                cursor++;
                var hasDigits = false;
                while (TryPeek(source, cursor, out var c) && Uri.IsHexDigit(c))
                {
                    hasDigits = true;
                    cursor++;
                }

                if (hasDigits)
                {
                    return state with
                    {
                        Cursor = cursor,
                        HasDigits = true
                    };
                }
            }
        }

        return state;
    }

    private static State ReadDecimal(string source, State state)
    {
        var cursor = state.Cursor;
        var hasDigit = false;
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
                HasDigits = true
            };
        }

        return state;
    }
    private static State ReadSuffix(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var s) && s is 'u' or 'U' or 'l' or 'L')
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
}
