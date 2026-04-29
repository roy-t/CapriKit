using static CapriKit.Generators.HLSL.Tokenizer.TokenizerUtils;

namespace CapriKit.Generators.HLSL.Tokenizer;

public static class CharacterTokenizer
{
    private const char Delimiter = '\'';
    private readonly record struct State(int Cursor, bool HasOpening, bool HasEscapeSequence, bool HasCharacter, bool HasClosing);

    /// <summary>
    /// Reads character literals.
    /// </summary>
    /// <seealso href="https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-appendix-grammar#characters"/>
    public static int ReadCharacter(string source, int offset, List<Token> tokens)
    {
        var state = new State(offset, false, false, false, false);

        state = ReadOpening(source, state);
        state = ReadEscapeSequence(source, state);
        state = ReadRegularCharacter(source, state);
        state = ReadClosing(source, state);

        if (state.HasOpening && state.HasClosing && (state.HasEscapeSequence ^ state.HasCharacter))
        {
            var length = state.Cursor - offset;
            tokens.Add(new Token(source, offset, length, TokenKind.Character));
            return length;
        }

        return 0;
    }

    private static State ReadOpening(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var c) && c == Delimiter)
        {
            return state with
            {
                HasOpening = true,
                Cursor = cursor + 1
            };
        }
        return state;
    }

    private static State ReadRegularCharacter(string source, State state)
    {
        // A delimeter can only exist in an escape sequence or at the start/end.
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var c) && c != Delimiter)
        {
            return state with
            {
                HasCharacter = true,
                Cursor = cursor + 1
            };
        }
        return state;
    }

    private static State ReadEscapeSequence(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var c) && c == '\\')
        {
            cursor++;
            if (TryPeek(source, cursor, out var escapee))
            {
                // Octal
                if (CountOctalSequence(source, cursor) == 3)
                {
                    // An octal escape sequence has exactly 3 digits
                    cursor += 3;
                    return state with
                    {
                        HasEscapeSequence = true,
                        Cursor = cursor
                    };
                }
                // Hex
                else if (escapee == 'x')
                {
                    // A hex escape sequence has at least 1 digit, but can have any number of them
                    cursor++;
                    var digits = CountDigitSequence(source, cursor);
                    if (digits > 0)
                    {
                        cursor += digits;
                        return state with
                        {
                            HasEscapeSequence = true,
                            Cursor = cursor
                        };
                    }
                }
                // Any other character signifies a regular one character escape sequence
                else
                {
                    cursor++;
                    return state with
                    {
                        HasEscapeSequence = true,
                        Cursor = cursor
                    };
                }
            }
        }

        return state;
    }

    private static State ReadClosing(string source, State state)
    {
        var cursor = state.Cursor;
        if (TryPeek(source, cursor, out var c) && c == Delimiter)
        {
            return state with
            {
                HasClosing = true,
                Cursor = cursor + 1
            };
        }
        return state;
    }
}
