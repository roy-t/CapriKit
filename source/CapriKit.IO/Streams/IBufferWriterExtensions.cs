using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace CapriKit.IO.Streams;

public static class BufferWriterExtensions
{
    /// <summary>
    /// Writes a length prefixed string. The prefix is the byte count of the encoded string as a
    /// 7 bit encoded integer, identical to <see cref="BinaryWriter.Write(string)"/>
    /// </summary>
    /// <param name="writer">The writer to write the string to</param>
    /// <param name="value">The string to write</param>
    /// <param name="encoding">Defaults to UTF8</param>
    public static void Write(this IBufferWriter<byte> writer, string value, Encoding? encoding = null)
    {
        encoding = encoding ?? Encoding.UTF8;
        var length = encoding.GetByteCount(value);
        writer.Write7BitEncodedInt(length);
        encoding.GetBytes(value, writer);
    }

    /// <summary>
    /// Writes an integer seven bits at a time, using the eighth bit to indicate that another byte
    /// follows. The format is identical to <see cref="BinaryWriter.Write7BitEncodedInt(int)"/>
    /// </summary>
    public static void Write7BitEncodedInt(this IBufferWriter<byte> writer, int value)
    {
        const int MaxLengthInBytes = 5; // ceil(32 bits / 7 bits per byte)

        var span = writer.GetSpan(MaxLengthInBytes);
        var written = 0;

        var uValue = (uint)value;
        while (uValue > 0x7Fu)
        {
            span[written++] = (byte)(uValue | ~0x7Fu);
            uValue >>= 7;
        }
        span[written++] = (byte)uValue;

        writer.Advance(written);
    }

    public static void Write(this IBufferWriter<byte> writer, int value)
    {
        var span = writer.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
        writer.Advance(sizeof(int));
    }

    public static void Write(this IBufferWriter<byte> writer, long value)
    {
        var span = writer.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
        writer.Advance(sizeof(long));
    }

    public static void Write(this IBufferWriter<byte> writer, Guid guid)
    {
        var span = writer.GetSpan(Unsafe.SizeOf<Guid>());
        guid.TryWriteBytes(span, false, out var written);
        writer.Advance(written);
    }
}
