using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class VirtualFileSystemSpyTests
{
    [Test]
    public async Task OpenedFiles()
    {
        var openedFile = new FilePath(@"D:\test_a.tmp");
        var untouchedFile = new FilePath(@"D:\test_b.tmp");
        var createdFile = new FilePath(@"D:\test_c.tmp");

        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateReadWrite(openedFile).Dispose();
        fileSystem.CreateReadWrite(untouchedFile).Dispose();

        var sut = fileSystem.SpyOn();

        sut.OpenRead(openedFile).Dispose();
        sut.CreateReadWrite(createdFile).Dispose();

        var actual = sut.OpenedFiles;
        HashSet<FilePath> expected = [openedFile, createdFile];

        var comparer = EqualityComparer<FilePath>.Create(
            (x, y) => object.Equals(x, y),
            obj => obj?.GetHashCode() ?? 0
        );

        await Assert.That(actual).IsEquivalentTo(expected, comparer);
    }
}
