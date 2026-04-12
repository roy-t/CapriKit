using static CapriKit.HLSL.TypeGenerator.Tokenizer.TokenizerUtils;

namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class NumberTokenizer
{
    private readonly record struct State(int Cursor, bool HasDecimalPoint, bool HasExponent, bool HasDigits, bool HasSuffix);

    // TODO: Add support for Hexadecimal and Octal numbers
    // TODO: Integers should not support exponents

    /// <summary>
    /// Parse both floating point and integer numbers. Emits an extra token if the number ends with a suffix.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#floating-point-numbers"/>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#integer-numbers" />
    public static int ReadNumber(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false, false, false);
        state = ReadIntegerPart(source, state);

        if (state.HasDigits)
        {
            var length = state.Cursor - offset;
            var numberKind = state.HasDecimalPoint
                ? TokenKind.FloatingPointLiteral
                : TokenKind.IntegerLiteral;

            if (state.HasSuffix)
            {
                tokens.Add(new Token(source, offset, length - 1, numberKind));
                tokens.Add(new Token(source, offset + length - 1, 1, TokenKind.NumberSuffix));
            }
            else
            {
                tokens.Add(new Token(source, offset, length, numberKind));
            }

            return length;
        }

        return 0;
    }

    private static State ReadIntegerPart(string source, State state)
    {
        // The integer part is optional
        var cursor = state.Cursor;
        var hasDigit = false;
        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            hasDigit = true;
            cursor++;
        }

        var nextState = state with
        {
            Cursor = cursor,
            HasDigits = hasDigit,
        };
        return ReadFractionalPart(source, nextState);
    }

    private static State ReadFractionalPart(string source, State state)
    {
        // The fractional part is optional.
        // If it exists, the dot does not have to be preceeded by 0 or another digit
        var cursor = state.Cursor;
        var hasDecimalPoint = false;
        var hasDigit = false;
        if (TryPeek(source, cursor, out var dot) && dot == '.')
        {
            cursor++;
            hasDecimalPoint = true;
            while (TryPeek(source, cursor, out var digit) && char.IsDigit(digit))
            {
                hasDigit = true;
                cursor++;
            }
        }

        var nextState = state with
        {
            Cursor = cursor,
            HasDecimalPoint = hasDecimalPoint,
            HasDigits = state.HasDigits || hasDigit
        };
        return ReadExponent(source, nextState);
    }

    private static State ReadExponent(string source, State state)
    {
        // An exponent is only valid only exist if we have seen a digit before
        if (!state.HasDigits)
        {
            return state;
        }

        var hasDigit = false;
        var cursor = state.Cursor;

        if (TryPeek(source, cursor, out var e) && (e == 'e' || e == 'E'))
        {
            cursor++;
            if (TryPeek(source, cursor, out var sign) && (sign == '-' || sign == '+'))
            {
                cursor++;
            }

            while (TryPeek(source, cursor, out var digit) && char.IsDigit(digit))
            {
                hasDigit = true;
                cursor++;
            }

            if (hasDigit)
            {
                var nextState = state with
                {
                    Cursor = cursor,
                    HasExponent = true
                };
                return ReadSuffix(source, nextState);
            }
        }

        return state;
    }

    private static State ReadSuffix(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var s))
        {
            // Allow 'half' and 'float' suffixes for floating point number and
            // 'long' and 'unsigned' suffixes for integers.
            if ((state.HasDecimalPoint && s is 'h' or 'H' or 'f' or 'F') ||
                !state.HasDecimalPoint && s is 'l' or 'L' or 'u' or 'U')
            {
                return state with
                {
                    Cursor = cursor + 1,
                    HasSuffix = true
                };
            }
        }

        return state;
    }
}
