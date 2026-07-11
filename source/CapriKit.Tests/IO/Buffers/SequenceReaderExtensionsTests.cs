using CapriKit.IO.Buffers;
using System.Buffers;
using System.Text;

namespace CapriKit.Tests.IO.Buffers;

internal class SequenceReaderExtensionsTests
{
    // SequenceReader<byte> is a ref struct, so all reading happens before the first
    // await and only plain locals cross into the assertions

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
}
