using CapriKit.Generators.HLSL.Builder;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class SourceCodeUtilsTests
{
    [Test]
    public async Task CreateValidTypeIdentifierUsesPascalCase()
    {
        await Assert.That(SourceCodeUtils.CreateValidTypeIdentifier("VS_INPUT")).IsEqualTo("VsInput");
    }

    [Test]
    public async Task CreateValidVariableIdentifierUsesCamelCase()
    {
        await Assert.That(SourceCodeUtils.CreateValidVariableIdentifier("World_Matrix")).IsEqualTo("worldMatrix");
    }

    [Test]
    public async Task CreateValidNamespaceJoinsPathSegments()
    {
        await Assert.That(SourceCodeUtils.CreateValidNamespace("utils/sub")).IsEqualTo("Utils.Sub");
    }

    [Test]
    public async Task GetRelativePathReturnsForwardSlashRelativePath()
    {
        var result = SourceCodeUtils.GetRelativePath("C:/x/Assets", "C:/x/Assets/sub/file.hlsl");
        await Assert.That(result).IsEqualTo("sub/file.hlsl");
    }

    [Test]
    public async Task ToLiteralQuotesStringsAndFormatsNumbers()
    {
        await Assert.That(SourceCodeUtils.ToLiteral("POSITION")).IsEqualTo("\"POSITION\"");
        await Assert.That(SourceCodeUtils.ToLiteral(3u)).IsEqualTo("3");
    }
}
