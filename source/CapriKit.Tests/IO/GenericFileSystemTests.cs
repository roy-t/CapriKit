using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class GenericFileSystemTests
{
    private FilePath File = null!;

    [Before(Test)]
    public void TestSetup()
    {
        var id = Path.GetRandomFileName();
        File = FilePath.FromSpan($"{nameof(GenericFileSystemTests)}.{id}.tmp");
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
    public async Task Delete(IVirtualFileSystem sut)
    {
        using (var writeStream = sut.CreateReadWrite(File)) { }
        await Assert.That(sut.Exists(File)).IsTrue();
        sut.Delete(File);
        await Assert.That(sut.Exists(File)).IsFalse();
    }
}
