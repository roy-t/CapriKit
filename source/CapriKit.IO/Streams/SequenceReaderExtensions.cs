using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CapriKit.IO.Streams;

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
    /// Creates a new reader to read a slice of the unread sequence. Advances the original reader.
    /// Use this method when you want to delegate reading a part of the sequence to another method
    /// without giving it access to the entire sequence.
    /// </summary>
    public static SequenceReader<byte> SliceUnread(ref this SequenceReader<byte> reader, int length)
    {
        if (!reader.TryReadExact(length, out var slice))
        {
            throw new EndOfStreamException();
        }

        return new SequenceReader<byte>(slice);
    }

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

    public static uint ReadUInt32(this ref SequenceReader<byte> reader)
    {
        return unchecked((uint)reader.ReadInt32());
    }

    public static float ReadSingle(this ref SequenceReader<byte> reader)
    {
        return BitConverter.Int32BitsToSingle(reader.ReadInt32());
    }

    public static Vector2 ReadVector2(this ref SequenceReader<byte> reader)
    {
        return new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }

    public static Vector3 ReadVector3(this ref SequenceReader<byte> reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public static Vector4 ReadVector4(this ref SequenceReader<byte> reader)
    {
        return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    /// <summary>
    /// Reads 16 floats stored row by row (M11, M12, M13, M14, M21, ...).
    /// This is the same layout <see cref="Matrix4x4"/> has in memory.
    /// </summary>
    public static Matrix4x4 ReadMatrix4x4RowMajor(this ref SequenceReader<byte> reader)
    {
        return new Matrix4x4(
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
            reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    /// <summary>
    /// Reads 16 floats stored column by column (M11, M21, M31, M41, M12, ...).
    /// </summary>
    public static Matrix4x4 ReadMatrix4x4ColumnMajor(this ref SequenceReader<byte> reader)
    {
        return Matrix4x4.Transpose(reader.ReadMatrix4x4RowMajor());
    }

    public static long ReadInt64(this ref SequenceReader<byte> reader)
    {
        if (!reader.TryReadLittleEndian(out long value))
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

    /// <summary>
    /// Blits an array of unmanaged structs. Assumes that struct's
    /// in-memory layout and endianness match the data's layout and endianness.
    /// </summary>    
    public static T[] BlitArray<T>(this ref SequenceReader<byte> reader, int count)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count, nameof(count));

        var array = GC.AllocateUninitializedArray<T>(count);
        var bytes = MemoryMarshal.AsBytes(array.AsSpan());
        if (!reader.TryCopyTo(bytes))
        {
            throw new EndOfStreamException();
        }
        reader.Advance(bytes.Length);
        return array;
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
