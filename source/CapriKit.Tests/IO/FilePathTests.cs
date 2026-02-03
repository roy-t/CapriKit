using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class FilePathTests
{
    private readonly FilePath AbsolutePath = new("C:/Windows/System32/config.ini");
    private readonly FilePath RelativePath = new("Windows/System32/config.ini");

    [Test]
    public async Task FileName()
    {
        await Assert.That(AbsolutePath.FileName.ToString()).IsEqualTo("config.ini");
    }

    [Test]
    public async Task FileNameWithoutExtension()
    {
        await Assert.That(AbsolutePath.FileNameWithoutExtension.ToString()).IsEqualTo("config");
    }

    [Test]
    public async Task Extension()
    {
        await Assert.That(AbsolutePath.Extension.ToString()).IsEqualTo(".ini");
    }

    [Test]
    public async Task Directory()
    {
        var directory = new DirectoryPath("C:/Windows/System32/");
        await Assert.That(AbsolutePath.Directory).IsEqualTo(directory);
    }

    [Test]
    public async Task IsAbsolute()
    {
        await Assert.That(AbsolutePath.IsAbsolute).IsTrue();
        await Assert.That(RelativePath.IsAbsolute).IsFalse();
    }

    [Test]
    public async Task GetPathRelativeTo()
    {
        var parentPath = new DirectoryPath("C:/");
        await Assert.That(AbsolutePath.GetPathRelativeTo(parentPath)).IsEqualTo(RelativePath);

        var badParentPath = new DirectoryPath("D:/");
        await Assert.That(() => AbsolutePath.GetPathRelativeTo(badParentPath)).Throws<Exception>();
    }

    [Test]
    public async Task StartsWith()
    {
        var beginning = new DirectoryPath("C:/Windows");
        await Assert.That(AbsolutePath.StartsWith(beginning)).IsTrue();
        await Assert.That(AbsolutePath.StartsWith("D:/")).IsFalse();
    }

    [Test]
    public async Task Contains()
    {
        var segment = new DirectoryPath("Windows");
        await Assert.That(AbsolutePath.Contains(segment)).IsTrue();
        await Assert.That(AbsolutePath.Contains("My Documents")).IsFalse();
    }

    [Test]
    public async Task EndsWith()
    {
        var ending = new DirectoryPath("System32");
        await Assert.That(AbsolutePath.EndsWith(RelativePath)).IsTrue();
        await Assert.That(AbsolutePath.EndsWith(AbsolutePath)).IsTrue();
        await Assert.That(AbsolutePath.EndsWith("C:/Windows")).IsFalse();
    }

    [Test]
    public async Task Equals()
    {
        var alternativePath = new FilePath(@"C:\Windows\System32\config.ini");
        var badPath = new DirectoryPath(@"D:\");
        await Assert.That(AbsolutePath.Equals(alternativePath)).IsTrue();
        await Assert.That(AbsolutePath == alternativePath).IsTrue();

        await Assert.That(AbsolutePath.Equals(badPath)).IsFalse();
        await Assert.That(AbsolutePath == badPath).IsFalse();
    }
}
