namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

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
    /// Consumes all characters remaining on this line. If this is not the last line in the file
    /// it will also consume the \n character itself.
    /// </summary>
    public static int AdvancePastEndOfLine(string source, int offset)
    {
        var advanced = 0;
        var cursor = offset + advanced;
        while (cursor < source.Length && source[cursor] != '\n')
        {
            advanced++;
            cursor = offset + advanced;
        }

        if (cursor + 1 < source.Length)
        {
            advanced++;
        }

        return advanced;
    }
}
