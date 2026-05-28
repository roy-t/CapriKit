using CapriKit.IO;

namespace CapriKit.Tests.IO;

// Note: most tests for this calls are in IVirtualFileSystemTests.cs
internal class ScopedFileSystemTests
{
    [Test]
    public async Task Accessing_File_Outside_Base_Path_Throws()
    {
        var basePath = new DirectoryPath(Path.Join(Path.GetTempPath(), "ScopedPath"));
        var fileSystem = new InMemoryFileSystem();
        var sut = new ScopedFileSystem(fileSystem, basePath);

        var forbiddenPath = new FilePath("../forbidden.txt");

        await Assert.That(() =>
        {
            sut.Exists(forbiddenPath);
        }).Throws<ForbiddenPathException>();
    }
}
