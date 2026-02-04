using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class DirectoryPathTests
{
    private readonly DirectoryPath AbsolutePath = new("C:/Windows/System32");
    private readonly DirectoryPath RelativePath = new("Windows/System32");

    [Test]
    public async Task IsAbsolute()
    {
        await Assert.That(AbsolutePath.IsAbsolute).IsTrue();
        await Assert.That(RelativePath.IsAbsolute).IsFalse();
    }

    [Test]
    public async Task Parent()
    {
        var parentPath = new DirectoryPath("C:/Windows");
        await Assert.That(AbsolutePath.Parent).IsEqualTo(parentPath);
    }

    [Test]
    public async Task ToAbsolute()
    {
        var parentPath = new DirectoryPath("C:/");
        await Assert.That(RelativePath.ToAbsolute(parentPath)).IsEqualTo(AbsolutePath);
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
        await Assert.That(AbsolutePath.StartsWith(AbsolutePath)).IsTrue();
        await Assert.That(AbsolutePath.StartsWith("D:/")).IsFalse();
    }

    [Test]
    public async Task Contains()
    {
        var segment = new DirectoryPath("Windows");
        await Assert.That(AbsolutePath.Contains(segment)).IsTrue();
        await Assert.That(AbsolutePath.Contains(AbsolutePath)).IsTrue();
        await Assert.That(AbsolutePath.Contains("My Documents")).IsFalse();
    }

    [Test]
    public async Task EndsWith()
    {
        var ending = new DirectoryPath("System32");
        await Assert.That(AbsolutePath.EndsWith(ending)).IsTrue();
        await Assert.That(AbsolutePath.EndsWith(AbsolutePath)).IsTrue();
        await Assert.That(AbsolutePath.EndsWith("C:/Windows")).IsFalse();
    }

    [Test]
    public async Task Join()
    {
        var subPath = new DirectoryPath("Com");
        var joinedWithSubPath = new DirectoryPath("C:/Windows/System32/Com");
        await Assert.That(AbsolutePath.Append([subPath])).IsEqualTo(joinedWithSubPath);

        var fileName = new FilePath("config.cfg");
        var joinedWithFileName = new FilePath("C:/Windows/System32/config.cfg");
        await Assert.That(AbsolutePath.Append(fileName)).IsEqualTo(joinedWithFileName);
    }

    [Test]
    public async Task Equals()
    {
        var alternativePath = new DirectoryPath(@"C:\Windows\System32\");
        var badPath = new DirectoryPath(@"D:\");
        await Assert.That(AbsolutePath.Equals(alternativePath)).IsTrue();
        await Assert.That(AbsolutePath == alternativePath).IsTrue();

        await Assert.That(AbsolutePath.Equals(badPath)).IsFalse();
        await Assert.That(AbsolutePath == badPath).IsFalse();
    }
}
