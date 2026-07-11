using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace CapriKit.IO.Buffers;

public static class SequenceReaders
{
    public static SequenceReader<byte> Create(byte[] bytes, int start, int length)
    {
        var sequence = new ReadOnlySequence<byte>(bytes, start, length);
        return new SequenceReader<byte>(sequence);
    }
}

public static class SequenceReaderExtensions
{


    /// <summary>
    /// Reads a length prefixed string written by
    /// <see cref="BufferWriterExtensions.Write(IBufferWriter{byte}, string, Encoding?)"/>.
    /// The format is identical to <see cref="BinaryReader.ReadString"/> so both can be mixed
    /// freely, as long as both sides use the same encoding
    /// </summary>
    /// <param name="reader">The reader to read the string from</param>
    /// <param name="encoding">Defaults to UTF8</param>
    public static string ReadString(this ref SequenceReader<byte> reader, Encoding? encoding = null)
    {
        encoding = encoding ?? Encoding.UTF8;
        var length = reader.Read7BitEncodedInt();
        if (!reader.TryReadExact(length, out var sequence))
        {
            throw new EndOfStreamException();
        }

        return encoding.GetString(in sequence);
    }

    /// <summary>
    /// Reads an integer written seven bits at a time by
    /// <see cref="BufferWriterExtensions.Write7BitEncodedInt(IBufferWriter{byte}, int)"/>
    /// or <see cref="BinaryWriter.Write7BitEncodedInt(int)"/>
    /// </summary>
    public static int Read7BitEncodedInt(this ref SequenceReader<byte> reader)
    {
        const int MaxBytesWithoutOverflow = 4;

        var result = 0u;
        for (var shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
        {
            var current = ReadByte(ref reader);
            result |= (current & 0x7Fu) << shift;
            if (current <= 0x7Fu)
            {
                return (int)result;
            }
        }

        // The fifth byte can only hold the 4 remaining bits of a 32 bit integer
        var last = ReadByte(ref reader);
        if (last > 0b_1111u)
        {
            throw new FormatException("Invalid 7 bit encoded integer");
        }

        result |= (uint)last << (MaxBytesWithoutOverflow * 7);
        return (int)result;
    }

    public static int ReadInt32(this ref SequenceReader<byte> reader)
    {
        if (!reader.TryReadLittleEndian(out int value))
        {
            throw new EndOfStreamException();
        }

        return value;
    }

    public static Guid ReadGuid(this ref SequenceReader<byte> reader)
    {
        Span<byte> bytes = stackalloc byte[Unsafe.SizeOf<Guid>()];
        if (!reader.TryCopyTo(bytes))
        {
            throw new EndOfStreamException();
        }

        reader.Advance(bytes.Length);
        return new Guid(bytes, bigEndian: false);
    }

    public static byte[] ReadBytes(this ref SequenceReader<byte> reader, int length)
    {
        if (!reader.TryReadExact(length, out var sequence))
        {
            throw new EndOfStreamException();
        }

        return sequence.ToArray();
    }

    public static byte ReadByte(ref SequenceReader<byte> reader)
    {
        if (!reader.TryRead(out var value))
        {
            throw new EndOfStreamException();
        }

        return value;
    }
}
