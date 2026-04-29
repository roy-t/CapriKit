using CapriKit.Generators.HLSL.Tokenizer;
using System.Text;

namespace CapriKit.Tests.Generators.HLSL.Tokenizer;

internal class StringTokenizerTests
{
    [Test]
    public async Task ParseString()
    {
        var input = "\"Hello World\"";
        var list = new List<Token>(1);
        var consumed = StringTokenizer.ReadString(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.String);
    }

    [Test]
    public async Task ParseStringWithEscapedEnding()
    {
        var builder = new StringBuilder();
        builder.Append('"');
        builder.Append("Hello");
        builder.Append('\\');
        builder.Append('"');
        builder.Append("World");
        builder.Append('"');
        var input = builder.ToString();
        var list = new List<Token>(1);
        var consumed = StringTokenizer.ReadString(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.String);
    }

    [Test]
    public async Task DoNotParseUnfinishedString()
    {
        var input = "\"Hello World";
        var list = new List<Token>(1);
        var consumed = StringTokenizer.ReadString(input, 0, list);
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(list).Count().IsEqualTo(0);
    }
}
