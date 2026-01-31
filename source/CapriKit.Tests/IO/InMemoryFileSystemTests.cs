using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class InMemoryFileSystemTests
{
    private readonly static FilePath File = FilePath.FromSpan(@"C:\Users\User1\Notes.txt");

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
}
