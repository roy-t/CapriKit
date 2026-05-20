using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

internal static class IntegerTokenizer
{
    /// <summary>
    /// Parse integers. Emits an extra token if the number ends with a suffix.
    /// </summary>
    /// <remarks>Always try to parse a character sequence as a floating point number before trying to parse it as an integer. If not the sequence "1.0" will be parsed as 'Int 1' and 'Float .0' </remarks>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#integer-numbers" />
    public static int ReadInteger(string source, int offset, List<Token> tokens)
    {
        var cursor = ReadIntegerConstant(source, offset);
        if (cursor > offset)
        {
            cursor = ReadSuffix(source, cursor);
            var length = cursor - offset;

            tokens.Add(new Token(source, offset, length, TokenKind.IntegerLiteral));

            return length;
        }

        return 0;
    }

    private static int ReadIntegerConstant(string source, int cursor)
    {
        var hex = ReadHex(source, cursor);
        if (hex > cursor) { return hex; }
        var octal = ReadOctal(source, cursor);
        if (octal > cursor) { return octal; }
        return ReadDecimal(source, cursor);
    }

    private static int ReadOctal(string source, int cursor)
    {
        var start = cursor;
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
                return cursor;
            }
        }

        return start;
    }

    private static int ReadHex(string source, int cursor)
    {
        var start = cursor;
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
                    return cursor;
                }
            }
        }

        return start;
    }

    private static int ReadDecimal(string source, int cursor)
    {
        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            cursor++;
        }

        return cursor;
    }

    private static int ReadSuffix(string source, int cursor)
    {
        if (TryPeek(source, cursor, out var s) && s is 'u' or 'U' or 'l' or 'L')
        {
            cursor++;
        }

        return cursor;
    }
}
