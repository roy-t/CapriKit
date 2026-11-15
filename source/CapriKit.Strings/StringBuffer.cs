using System.Runtime.CompilerServices;

namespace CapriKit.Strings;

/// <summary>
/// Formats interpolated strings into a reusable buffer without allocating, the buffer only grows when the text
/// does not fit. Use it for text that is formatted every frame, such as debug labels or log messages.
/// Only values that implement <see cref="ISpanFormattable"/> can be used as holes.
/// Not thread-safe.
/// </summary>
public sealed class StringBuffer
{
    private readonly IFormatProvider? provider;
    private char[] buffer;
    private int length;

    /// <param name="capacity">Initial size of the buffer in characters.</param>
    /// <param name="provider">
    /// Culture used to format holes, or null to use the current culture.
    /// An <see cref="ICustomFormatter"/> supplied by the provider is ignored.
    /// </param>
    public StringBuffer(int capacity = 32, IFormatProvider? provider = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        buffer = new char[capacity];
        this.provider = provider;
    }

    private Span<char> Destination => buffer;

    /// <summary>
    /// Formats the interpolated string into the buffer and returns the result.
    /// The returned span is only valid until the next call to <see cref="Write"/>.
    /// </summary>
    public ReadOnlySpan<char> Write([InterpolatedStringHandlerArgument("")] scoped ref StringBufferHandler handler)
    {
        // A forwarded handler can belong to another buffer
        if (handler.Owner != this)
        {
            throw new ArgumentException($"The handler was created for a different {nameof(StringBuffer)}", nameof(handler));
        }

        length = handler.Length;
        return buffer.AsSpan(0, length);
    }

    public override string ToString()
    {
        return buffer.AsSpan(0, length).ToString();
    }

    private void Grow(int written, int required = 0)
    {
        var capacity = Math.Max(Math.Min(buffer.Length * 2L, Array.MaxLength), required);
        var larger = new char[capacity];
        buffer.AsSpan(0, written).CopyTo(larger);
        buffer = larger;
    }

    /// <summary>
    /// Created by the compiler when an interpolated string is passed to <see cref="Write"/>, do not use directly.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct StringBufferHandler
    {
        public StringBufferHandler(int literalLength, int formattedCount, StringBuffer owner)
        {
            Owner = owner;
        }

        public int Length { get; private set; }

        internal readonly StringBuffer Owner { get; }

        public void AppendLiteral(string value) => AppendFormatted(value.AsSpan());

        public void AppendFormatted(ReadOnlySpan<char> value) // also accepts string holes
        {
            if (Length + value.Length > Owner.Destination.Length)
            {
                Owner.Grow(Length, Length + value.Length);
            }

            value.CopyTo(Owner.Destination[Length..]);
            Length += value.Length;
        }

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        {
            var start = Length;
            AppendFormatted(value);
            Pad(start, alignment);
        }

        // Constraining to ISpanFormattable turns holes that would need ToString() or boxing into compile errors
        public void AppendFormatted<T>(T value) where T : ISpanFormattable => AppendFormatted(value, null);

        public void AppendFormatted<T>(T value, string? format) where T : ISpanFormattable
        {
            int written;
            while (!value.TryFormat(Owner.Destination[Length..], out written, format, Owner.provider))
            {
                Owner.Grow(Length);
            }
            Length += written;
        }

        public void AppendFormatted<T>(T value, int alignment) where T : ISpanFormattable => AppendFormatted(value, alignment, null);

        public void AppendFormatted<T>(T value, int alignment, string? format) where T : ISpanFormattable
        {
            var start = Length;
            AppendFormatted(value, format);
            Pad(start, alignment);
        }

        // Pads the text written since start with spaces up to the alignment width,
        // a positive alignment right-aligns the text and a negative alignment left-aligns it
        private void Pad(int start, int alignment)
        {
            var written = Length - start;
            var padding = Math.Abs(alignment) - written;
            if (padding <= 0)
            {
                return;
            }

            if (Length + padding > Owner.Destination.Length)
            {
                Owner.Grow(Length, Length + padding);
            }

            var destination = Owner.Destination;
            if (alignment > 0)
            {
                // CopyTo handles overlapping source and destination correctly
                destination.Slice(start, written).CopyTo(destination[(start + padding)..]);
                destination.Slice(start, padding).Fill(' ');
            }
            else
            {
                destination.Slice(Length, padding).Fill(' ');
            }

            Length += padding;
        }
    }
}
