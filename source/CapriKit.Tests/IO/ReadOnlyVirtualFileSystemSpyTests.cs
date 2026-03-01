using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class ReadOnlyVirtualFileSystemSpyTests
{
    [Test]
    public async Task OpenedFiles()
    {
        var openedFile = new FilePath(@"D:\test_a.tmp");
        var untouchedFile = new FilePath(@"D:\test_b.tmp");

        var fileSystem = new InMemoryFileSystem();
        fileSystem.CreateReadWrite(openedFile).Dispose();
        fileSystem.CreateReadWrite(untouchedFile).Dispose();

        var sut = ((IReadOnlyVirtualFileSystem)fileSystem).SpyOn();

        sut.OpenRead(openedFile).Dispose();

        var actual = sut.OpenedFiles;
        HashSet<FilePath> expected = [openedFile];

        var comparer = EqualityComparer<FilePath>.Create(
            (x, y) => object.Equals(x, y),
            obj => obj?.GetHashCode() ?? 0
        );

        await Assert.That(actual).IsEquivalentTo(expected, comparer);
    }
}
