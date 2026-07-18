using CapriKit.IO;

namespace CapriKit.Tests.IO;

internal class IOUtilitiesTests
{
    [Test]
    public async Task NormalizePathSeparators()
    {
        var normalized = IOUtilities.NormalizePathSeparators(@"C:\Windows\System32");
        await Assert.That(normalized.ToString()).IsEqualTo("C:/Windows/System32");
    }

    [Test]
    public async Task NormalizeDotSegments()
    {
        var normalized = IOUtilities.NormalizeDotSegments(@"C:\Windows\System32\..\Fonts");
        await Assert.That(normalized.ToString()).IsEqualTo(@"C:\Windows\Fonts");
    }

    [Test]
    public async Task Normalize()
    {
        var normalized = IOUtilities.Normalize(@"C:\Windows\System32\..\Fonts");
        await Assert.That(normalized.ToString()).IsEqualTo("C:/Windows/Fonts");
    }

    [Test]
    public async Task AddTrailingDirectorySeparator()
    {
        var path = IOUtilities.AddTrailingDirectorySeparator("C:/Windows");
        await Assert.That(path.ToString()).IsEqualTo("C:/Windows/");
    }

    [Test]
    public async Task RemoveTrailingDirectorySeparator()
    {
        var path = IOUtilities.RemoveTrailingDirectorySeparator("C:/Windows/");
        await Assert.That(path.ToString()).IsEqualTo("C:/Windows");
    }

    [Test]
    public async Task IsValidFileName()
    {
        await Assert.That(IOUtilities.IsValidFileName("config.cfg")).IsTrue();
    }

    [Test]
    public async Task IsValidFileName_InvalidCharacter()
    {
        await Assert.That(IOUtilities.IsValidFileName("config?.cfg")).IsFalse();
    }

    [Test]
    public async Task IsValidFilePath()
    {
        await Assert.That(IOUtilities.IsValidFilePath("C:/Windows/config.cfg")).IsTrue();
    }

    [Test]
    public async Task IsValidFilePath_InvalidCharacter()
    {
        await Assert.That(IOUtilities.IsValidFilePath("C:/Windows/config?.cfg")).IsFalse();
    }

    [Test]
    public async Task IsValidPath()
    {
        await Assert.That(IOUtilities.IsValidPath("C:/Windows")).IsTrue();
    }

    [Test]
    public async Task IsValidPath_InvalidCharacter()
    {
        await Assert.That(IOUtilities.IsValidPath("C:/Win|dows")).IsFalse();
    }

    [Test]
    public async Task EscapeFileName()
    {
        var escaped = IOUtilities.EscapeFileName("shader.hlsl/VertexMain");
        await Assert.That(escaped.ToString()).IsEqualTo("shader.hlsl%2FVertexMain");
    }

    [Test]
    public async Task SearchForDirectoryWithMarker()
    {
        var id = Path.GetRandomFileName();
        var rootPath = Path.Combine(Path.GetTempPath(), $"{nameof(IOUtilitiesTests)}.{id}");
        var root = new DirectoryPath(rootPath);
        var nested = new DirectoryPath(Path.Combine(rootPath, "a", "b"));
        try
        {
            Directory.CreateDirectory(nested);
            File.WriteAllText(root.Append(new FilePath("marker.txt")), string.Empty);

            var found = IOUtilities.SearchForDirectoryWithMarker(nested, "marker.txt");

            await Assert.That(found).IsEqualTo(root);
        }
        finally
        {
            new DirectoryInfo(root).Delete(true);
        }
    }
}
