using CapriKit.IO;
using System.Text;

namespace CapriKit.Tests.IO;

internal class IVirtualFileSystemTests
{
    private FilePath File = null!;

    [Before(Test)]
    public void TestSetup()
    {
        var id = Path.GetRandomFileName();
        File = new FilePath($"{nameof(IVirtualFileSystemTests)}.{id}.tmp");
    }

    [After(Test)]
    public void TestTeardown()
    {
        var info = new FileInfo(File.ToString());
        info.Delete();
    }

    public static class FileSystemDataSource
    {
        public static IEnumerable<Func<IVirtualFileSystem>> Generator()
        {
            yield return () => new InMemoryFileSystem();
            yield return () => new ScopedFileSystem(new DirectoryPath(Path.GetTempPath()));
        }
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task AppendReadWrite(IVirtualFileSystem sut)
    {
        var part1 = "Hello";
        var part2 = "World";

        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);
            writer.Write(part1);
        }

        using (var appendStream = sut.AppendWrite(File))
        {
            using var writer = new StreamWriter(appendStream);
            writer.Write(part2);
        }


        var readText = await sut.ReadAllText(File);

        await Assert.That(readText).IsEqualTo(part1 + part2);
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task AppendReadWrite_ThrowsIfFileDoesNotExists(IVirtualFileSystem sut)
    {
        await Assert.That(() => sut.AppendWrite(File)).Throws<FileNotFoundException>();
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task CreateReadWrite(IVirtualFileSystem sut)
    {
        var writtenText = "Hello World";

        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);

            writer.Write(writtenText);
        }
        var readText = await sut.ReadAllText(File);

        await Assert.That(readText).IsEqualTo(writtenText);
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task Delete(IVirtualFileSystem sut)
    {
        using (var writeStream = sut.CreateReadWrite(File)) { }
        await Assert.That(sut.Exists(File)).IsTrue();
        sut.Delete(File);
        await Assert.That(sut.Exists(File)).IsFalse();
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task Exists(IVirtualFileSystem sut)
    {
        var exists = sut.Exists(File);
        await Assert.That(exists).IsFalse();

        using (var writeStream = sut.CreateReadWrite(File)) { }

        exists = sut.Exists(File);
        await Assert.That(exists).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task LastWriteTime(IVirtualFileSystem sut)
    {
        var start = DateTime.Now;

        var part1 = "Hello";
        var part2 = "World";

        using (var writeStream = sut.CreateReadWrite(File))
        {
            using var writer = new StreamWriter(writeStream);
            writer.Write(part1);
        }

        var before = sut.LastWriteTime(File);

        using (var appendStream = sut.AppendWrite(File))
        {
            using var writer = new StreamWriter(appendStream);
            writer.Write(part2);
        }

        var after = sut.LastWriteTime(File);

        // The resolution of DateTime.Now is ~1ms. Sometimes
        // this test is so fast that if we compare before and after
        // using less than. So we make the weaker assumption.
        await Assert.That(start).IsLessThanOrEqualTo(before);
        await Assert.That(before).IsLessThanOrEqualTo(after);
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task LastWriteTime_ThrowsIfFileDoesNotExists(IVirtualFileSystem sut)
    {
        await Assert.That(() => sut.LastWriteTime(File)).Throws<FileNotFoundException>();
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task OpenRead(IVirtualFileSystem sut)
    {
        var writtenText = "Hello World";

        await sut.WriteAllText(File, writtenText);

        using var stream = sut.OpenRead(File);
        using var reader = new StreamReader(stream);

        var readText = reader.ReadToEnd();

        await Assert.That(readText).IsEqualTo(writtenText);
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task OpenRead_ThrowsIfFileDoesNotExists(IVirtualFileSystem sut)
    {
        await Assert.That(() => sut.OpenRead(File)).Throws<FileNotFoundException>();
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task SizeInBytes(IVirtualFileSystem sut)
    {
        var bytes = Encoding.UTF8.GetBytes("Hello World");
        await sut.WriteAllBytes(File, bytes);

        var size = sut.SizeInBytes(File);

        await Assert.That(size).IsEqualTo(bytes.LongLength);
    }

    [Test]
    [MethodDataSource(typeof(FileSystemDataSource), nameof(FileSystemDataSource.Generator))]
    public async Task SizeInBytes_ThrowsIfFileDoesNotExists(IVirtualFileSystem sut)
    {
        await Assert.That(() => sut.SizeInBytes(File)).Throws<FileNotFoundException>();
    }
}
