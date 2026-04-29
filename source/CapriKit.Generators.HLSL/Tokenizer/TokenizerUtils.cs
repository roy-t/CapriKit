namespace CapriKit.Generators.HLSL.Tokenizer;

public static class TokenizerUtils
{
    /// <summary>
    /// Returns next character, if available.
    /// </summary>
    /// <returns>True if a character is available. False if trying to read past the end of the string</returns>
    public static bool TryPeek(string source, int cursor, out char c)
    {
        if (cursor < source.Length)
        {
            c = source[cursor];
            return true;
        }

        c = default;
        return false;
    }

    /// <summary>
    /// Consumes all characters remaining on this line
    /// </summary>
    public static int AdvanceToEndOfLine(string source, int offset)
    {
        var cursor = offset;
        while (TryPeek(source, cursor, out var c) && c != '\n')
        {
            cursor++;
        }
        return cursor - offset;
    }

    /// <summary>
    /// Counts how many consecutive digits are available in source, starting at offset
    /// </summary>
    public static int CountDigitSequence(string source, int offset)
    {
        var cursor = offset;
        while (TryPeek(source, cursor, out var d) && char.IsDigit(d))
        {
            cursor++;
        }

        return cursor - offset;
    }

    /// <summary>
    /// Counts how many consecutive octals are available in source, starting at offset
    /// </summary>
    public static int CountOctalSequence(string source, int offset)
    {
        var cursor = offset;
        while (TryPeek(source, cursor, out var d) && d >= '0' && d <= '7')
        {
            cursor++;
        }

        return cursor - offset;
    }
}
