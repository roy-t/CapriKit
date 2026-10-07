using CapriKit.IO.Streams;
using System.Text;

namespace CapriKit.Tests.IO.Streams;

internal class StreamExtensionsTests
{
    private static readonly uint[] Values = [1, 2, 0xDEADBEEF];

    [Test]
    public async Task BlitArrayAsync()
    {
        using var stream = new MemoryStream();
        Write(stream, Values);
        stream.Position = 0;

        var result = await stream.BlitArrayAsync<uint>(Values.Length);

        await Assert.That(result.SequenceEqual(Values)).IsTrue();
        await Assert.That(stream.Position).IsEqualTo(stream.Length);
    }

    [Test]
    public async Task BlitArrayAsync_AsyncFileStream()
    {
        var path = Path.GetTempFileName();
        try
        {
            using (var writeStream = File.OpenWrite(path))
            {
                Write(writeStream, Values);
            }

            // Unbuffered async I/O makes the stream pin the destination memory instead of copying via a span
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 0, useAsync: true);
            var result = await stream.BlitArrayAsync<uint>(Values.Length);

            await Assert.That(result.SequenceEqual(Values)).IsTrue();
            await Assert.That(stream.Position).IsEqualTo(stream.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void Write(Stream stream, uint[] values)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        foreach (var value in values)
        {
            writer.Write(value);
        }
    }
}
