using CapriKit.IO.Streams;
using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace CapriKit.Tests.IO.Streams;

internal class IBufferWriterExtensionsTests
{
    [Test]
    public async Task Write_Int32()
    {
        var writer = new ArrayBufferWriter<byte>();

        writer.Write(-12345);

        var bytes = writer.WrittenMemory.ToArray();
        var value = BinaryPrimitives.ReadInt32LittleEndian(bytes);

        await Assert.That(bytes.Length).IsEqualTo(sizeof(int));
        await Assert.That(value).IsEqualTo(-12345);
    }

    [Test]
    public async Task Write_String()
    {
        var writer = new ArrayBufferWriter<byte>();
        var value = "héllo"; // 'é' encodes to two bytes, so the prefix must count bytes, not chars

        writer.Write(value);

        // The format promises to be identical to BinaryWriter/BinaryReader's
        using var stream = new MemoryStream(writer.WrittenMemory.ToArray());
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        var text = reader.ReadString();

        await Assert.That(text).IsEqualTo(value);
        await Assert.That(stream.Position).IsEqualTo(stream.Length);
    }

    [Test]
    public async Task Write7BitEncodedInt()
    {
        int[] values = [0, 127, 128, 300, int.MaxValue, -1];
        var writer = new ArrayBufferWriter<byte>();
        foreach (var value in values)
        {
            writer.Write7BitEncodedInt(value);
        }

        // The format promises to be identical to BinaryWriter/BinaryReader's
        using var stream = new MemoryStream(writer.WrittenMemory.ToArray());
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        foreach (var value in values)
        {
            await Assert.That(reader.Read7BitEncodedInt()).IsEqualTo(value);
        }

        await Assert.That(stream.Position).IsEqualTo(stream.Length);
    }

    [Test]
    public async Task Write_Guid()
    {
        var writer = new ArrayBufferWriter<byte>();
        var guid = Guid.NewGuid();

        writer.Write(guid);

        var bytes = writer.WrittenMemory.ToArray();
        var roundTripped = new Guid(bytes, bigEndian: false);

        await Assert.That(bytes.Length).IsEqualTo(16);
        await Assert.That(roundTripped).IsEqualTo(guid);
    }
}
