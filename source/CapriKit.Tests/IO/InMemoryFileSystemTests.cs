using CapriKit.IO;
using System.Text;

namespace CapriKit.Tests.IO;

internal class InMemoryFileSystemTests
{
    private readonly static FilePath File = FilePath.FromSpan(@"C:\Users\User1\Notes.txt");

    [Test]
    public async Task AppendReadWrite()
    {
        var part1 = "Hello";
        var part2 = "World";

        var sut = new InMemoryFileSystem();
        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);
            writer.Write(part1);
        }

        using (var appendStream = sut.AppendReadWrite(File))
        {
            using var writer = new StreamWriter(appendStream);
            writer.Write(part2);
        }


        var readText = await sut.ReadAllText(File);

        await Assert.That(readText).IsEqualTo(part1 + part2);
    }

    [Test]
    public async Task AppendReadWrite_ThrowsIfFileDoesNotExists()
    {
        var sut = new InMemoryFileSystem();
        await Assert.That(() => sut.AppendReadWrite(File)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task CreateReadWrite()
    {
        var writtenText = "Hello World";

        var sut = new InMemoryFileSystem();
        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);

            writer.Write(writtenText);
        }
        var readText = await sut.ReadAllText(File);

        await Assert.That(readText).IsEqualTo(writtenText);
    }

    [Test]
    public async Task Delete()
    {
        var sut = new InMemoryFileSystem();
        using (var writeStream = sut.CreateReadWrite(File)) { }
        await Assert.That(sut.Exists(File)).IsTrue();
        sut.Delete(File);
        await Assert.That(sut.Exists(File)).IsFalse();
    }

    [Test]
    public async Task Exists()
    {
        var sut = new InMemoryFileSystem();
        var exists = sut.Exists(File);
        await Assert.That(exists).IsFalse();

        using (var writeStream = sut.CreateReadWrite(File)) { }

        exists = sut.Exists(File);
        await Assert.That(exists).IsTrue();
    }

    [Test]
    public async Task LastWriteTime()
    {
        var start = DateTime.Now;

        var part1 = "Hello";
        var part2 = "World";

        var sut = new InMemoryFileSystem();
        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);
            writer.Write(part1);
        }

        var before = sut.LastWriteTime(File);

        using (var appendStream = sut.AppendReadWrite(File))
        {
            using var writer = new StreamWriter(appendStream);
            writer.Write(part2);
        }

        var after = sut.LastWriteTime(File);

        await Assert.That(start).IsLessThan(before);
        await Assert.That(before).IsLessThan(after);
    }

    [Test]
    public async Task LastWriteTime_ThrowsIfFileDoesNotExists()
    {
        var sut = new InMemoryFileSystem();
        await Assert.That(() => sut.LastWriteTime(File)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task OpenRead()
    {
        var writtenText = "Hello World";

        var sut = new InMemoryFileSystem();
        await sut.WriteAllText(File, writtenText);

        using var stream = sut.OpenRead(File);
        using var reader = new StreamReader(stream);

        var readText = reader.ReadToEnd();

        await Assert.That(readText).IsEqualTo(writtenText);
    }

    [Test]
    public async Task OpenRead_ThrowsIfFileDoesNotExists()
    {
        var sut = new InMemoryFileSystem();
        await Assert.That(() => sut.OpenRead(File)).Throws<FileNotFoundException>();
    }

    [Test]
    public async Task SizeInBytes()
    {
        var sut = new InMemoryFileSystem();
        var bytes = Encoding.UTF8.GetBytes("Hello World");
        await sut.WriteAllBytes(File, bytes);

        var size = sut.SizeInBytes(File);

        await Assert.That(size).IsEqualTo(bytes.LongLength);
    }

    [Test]
    public async Task SizeInBytes_ThrowsIfFileDoesNotExists()
    {
        var sut = new InMemoryFileSystem();
        await Assert.That(() => sut.SizeInBytes(File)).Throws<FileNotFoundException>();
    }
}
