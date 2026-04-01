namespace CapriKit.HLSL.TypeGenerator.Tokenizer;

public static class WhitespaceTokenizer
{
    /// <summary>
    /// Reads whitespace
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#whitespace"/>
    public static int ReadWhitespace(string source, int offset)
    {
        var advanced = 0;
        var cursor = advanced + offset;
        while (cursor < source.Length && char.IsWhiteSpace(source[cursor]))
        {
            advanced++;
            cursor = offset + advanced;
        }

        return advanced;
    }
}
