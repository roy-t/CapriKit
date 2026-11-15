using System.Runtime.CompilerServices;

namespace CapriKit.Strings;

/// <summary>
/// An interpolator for interpolated strings that only allocates if the internal buffer needs to grow (similarly to how a <seealso cref="List{T}"/> behaves).
/// </summary>
public sealed class StringBuffer
{
    private char[] buffer;
    private int length;

    public StringBuffer(int capacity = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        buffer = new char[capacity];
    }

    private Span<char> Destination => buffer;

    // scoped: the returned span points into the buffer, never into the handler
    public ReadOnlySpan<char> Write([InterpolatedStringHandlerArgument("")] scoped ref StringBufferHandler handler)
    {
        length = handler.Length;
        return buffer.AsSpan(0, length);
    }

    public override string ToString()
    {
        return buffer.AsSpan(0, length).ToString();
    }

    private void Grow(int written)
    {
        var larger = new char[buffer.Length * 2];
        buffer.AsSpan(0, written).CopyTo(larger);
        buffer = larger;
    }


    [InterpolatedStringHandler]
    public ref struct StringBufferHandler
    {
        private readonly StringBuffer owner;

        public StringBufferHandler(int literalLength, int formattedCount, StringBuffer owner)
        {
            this.owner = owner;
        }

        public int Length { get; private set; }

        public void AppendLiteral(string value) => AppendFormatted(value.AsSpan());

        public void AppendFormatted(ReadOnlySpan<char> value) // also accepts string holes
        {
            while (!value.TryCopyTo(owner.Destination[Length..]))
            {
                owner.Grow(Length);
            }
            Length += value.Length;
        }

        // By only supporting patterns that are ISpanFormattable the compiler throws an error when a user
        // passes an item that might cause copies/allocations.
        public void AppendFormatted<T>(T value) where T : ISpanFormattable => AppendFormatted(value, null);

        public void AppendFormatted<T>(T value, string? format) where T : ISpanFormattable
        {
            int written;
            while (!value.TryFormat(owner.Destination[Length..], out written, format, provider: null))
            {
                owner.Grow(Length);
            }
            Length += written;
        }
    }
}
