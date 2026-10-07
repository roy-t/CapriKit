using CapriKit.IO.Streams;
using System.Buffers;
using System.Numerics;
using System.Text;

namespace CapriKit.Tests.IO.Streams;

internal class SequenceReaderExtensionsTests
{
    // SequenceReader<byte> is a ref struct, so all reading happens before the first
    // await and only plain locals cross into the assertions

    [Test]
    public async Task SliceUnread()
    {
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(111);
        writer.Write(222);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var slice = reader.SliceUnread(sizeof(int));
        var sliced = slice.ReadInt32();
        var sliceEnd = slice.End;
        var remaining = reader.ReadInt32();
        var end = reader.End;

        await Assert.That(sliced).IsEqualTo(111);
        await Assert.That(sliceEnd).IsTrue(); // the slice cannot see beyond its own section
        await Assert.That(remaining).IsEqualTo(222); // the original reader skipped the sliced section
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadInt32()
    {
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(-12345);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var value = reader.ReadInt32();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(-12345);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadUInt32()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(0xDEADBEEFu); // larger than int.MaxValue
        }

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
        var value = reader.ReadUInt32();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(0xDEADBEEFu);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadSingle()
    {
        var reader = CreateReader(-1.5f);
        var value = reader.ReadSingle();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(-1.5f);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadVector2()
    {
        var reader = CreateReader(1, 2);
        var value = reader.ReadVector2();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(new Vector2(1, 2));
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadVector3()
    {
        var reader = CreateReader(1, 2, 3);
        var value = reader.ReadVector3();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(new Vector3(1, 2, 3));
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadVector4()
    {
        var reader = CreateReader(1, 2, 3, 4);
        var value = reader.ReadVector4();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(new Vector4(1, 2, 3, 4));
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadMatrix4x4RowMajor()
    {
        var reader = CreateReader(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var value = reader.ReadMatrix4x4RowMajor();
        var end = reader.End;

        var expected = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        await Assert.That(value).IsEqualTo(expected);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadMatrix4x4ColumnMajor()
    {
        var reader = CreateReader(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var value = reader.ReadMatrix4x4ColumnMajor();
        var end = reader.End;

        var expected = new Matrix4x4(
            1, 5, 9, 13,
            2, 6, 10, 14,
            3, 7, 11, 15,
            4, 8, 12, 16);
        await Assert.That(value).IsEqualTo(expected);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadString()
    {
        var writer = new ArrayBufferWriter<byte>();
        writer.Write("héllo"); // 'é' encodes to two bytes, exercising the bytes-not-chars length prefix

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var value = reader.ReadString();
        var end = reader.End;

        await Assert.That(value).IsEqualTo("héllo");
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadString_WrittenByBinaryWriter()
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("héllo");
        }

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
        var value = reader.ReadString();
        var end = reader.End;

        await Assert.That(value).IsEqualTo("héllo");
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task Read7BitEncodedInt()
    {
        int[] values = [0, 127, 128, 300, int.MaxValue, -1];
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var value in values)
            {
                writer.Write7BitEncodedInt(value);
            }
        }

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
        var results = new List<int>();
        while (!reader.End)
        {
            results.Add(reader.Read7BitEncodedInt());
        }

        await Assert.That(results.SequenceEqual(values)).IsTrue();
    }

    [Test]
    public async Task ReadGuid()
    {
        var guid = Guid.NewGuid();
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(guid);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var value = reader.ReadGuid();
        var end = reader.End;

        await Assert.That(value).IsEqualTo(guid);
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task BlitArray()
    {
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(1.0f);
        writer.Write(2.0f);
        writer.Write(3.0f);
        writer.Write(4.0f);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var values = reader.BlitArray<Vector2>(2);
        var end = reader.End;

        await Assert.That(values.Length).IsEqualTo(2);
        await Assert.That(values[0]).IsEqualTo(new Vector2(1.0f, 2.0f));
        await Assert.That(values[1]).IsEqualTo(new Vector2(3.0f, 4.0f));
        await Assert.That(end).IsTrue();
    }

    [Test]
    public async Task ReadBytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var writer = new ArrayBufferWriter<byte>();
        writer.Write(bytes);

        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
        var value = reader.ReadBytes(bytes.Length);
        var end = reader.End;

        await Assert.That(value.SequenceEqual(bytes)).IsTrue();
        await Assert.That(end).IsTrue();
    }

    private static SequenceReader<byte> CreateReader(params float[] values)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            foreach (var value in values)
            {
                writer.Write(value);
            }
        }

        return new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
    }
}
