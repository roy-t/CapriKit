namespace CapriKit.HLSL.TypeGenerator.Parsers;

public class NumberTokenizer
{
    private readonly record struct State(int Cursor, bool HasDecimalPoint, bool HasExponent, bool HasDigits, bool HasSuffix);

    public int Advance(string source, int offset, List<Token> tokens)
    {
        return ReadNumber(source, offset, tokens);
    }

    public static int ReadNumber(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false, false, false);
        state = ReadIntegerPart(source, state);

        if (state.HasDigits)
        {
            var length = state.Cursor - offset;

            if (state.HasSuffix)
            {
                tokens.Add(new Token(source, offset, length - 1, TokenKind.Number));
                tokens.Add(new Token(source, offset + length - 1, 1, TokenKind.NumberSuffix));
            }
            else
            {
                tokens.Add(new Token(source, offset, length, TokenKind.Number));
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
        if (TryPeek(source, cursor, out var s) &&
            s is 'h' or 'H' or 'f' or 'F' or 'l' or 'L')
        {
            return state with
            {
                Cursor = cursor + 1,
                HasSuffix = true
            };
        }

        return state;
    }

    private static bool TryPeek(string source, int cursor, out char c)
    {
        if (cursor < source.Length)
        {
            c = source[cursor];
            return true;
        }

        c = default;
        return false;
    }
}
