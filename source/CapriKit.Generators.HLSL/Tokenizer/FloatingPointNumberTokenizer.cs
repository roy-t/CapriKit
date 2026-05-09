using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class FloatingPointNumberTokenizer
{
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
        var cursor = ReadFractionalConstant(source, offset);
        if (cursor == offset) { return 0; }

        cursor = ReadExponent(source, cursor);
        cursor = ReadSuffix(source, cursor);

        return CreateTokens(source, offset, tokens, cursor);
    }

    private static int ReadFloatingPointNumberDigitSequence(string source, int offset, List<Token> tokens)
    {
        var cursor = ReadDigitSequence(source, offset);
        if (cursor == offset) { return 0; }

        var afterDigits = cursor;
        cursor = ReadExponent(source, cursor);
        if (cursor == afterDigits) { return 0; }

        cursor = ReadSuffix(source, cursor);
        return CreateTokens(source, offset, tokens, cursor);
    }

    private static int CreateTokens(string source, int offset, List<Token> tokens, int cursor)
    {
        var length = cursor - offset;
        tokens.Add(new Token(source, offset, length, TokenKind.FloatingPointLiteral));
        return length;
    }

    private static int ReadFractionalConstant(string source, int cursor)
    {
        var start = cursor;

        // The integer part is optional
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
            return cursor;
        }

        return start;
    }

    private static int ReadDigitSequence(string source, int cursor)
    {
        while (TryPeek(source, cursor, out var c) && char.IsDigit(c))
        {
            cursor++;
        }

        return cursor;
    }

    private static int ReadExponent(string source, int cursor)
    {
        var start = cursor;
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
                return cursor;
            }
        }

        return start;
    }

    private static int ReadSuffix(string source, int cursor)
    {
        if (TryPeek(source, cursor, out var s) && s is 'h' or 'H' or 'f' or 'F' or 'l' or 'L')
        {
            cursor++;
        }

        return cursor;
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
