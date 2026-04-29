using CapriKit.Generators.HLSL.Tokenizer;

namespace CapriKit.Tests.Generators.HLSL;

internal class CommentTokenizerTests
{
    [Test]
    public async Task ParseBlockCommentAtEndOfInput()
    {
        var input = "/* abc */";
        var list = new List<Token>(1);
        var consumed = CommentTokenizer.ReadComment(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Comment);
    }

    [Test]
    public async Task ParseLineComment()
    {
        var input = "// hello world";
        var list = new List<Token>(1);
        var consumed = CommentTokenizer.ReadComment(input, 0, list);
        await Assert.That(consumed).IsEqualTo(input.Length);
        await Assert.That(list).Count().IsEqualTo(1);
        await Assert.That(list[0].Value).IsEqualTo(input);
        await Assert.That(list[0].Kind).IsEqualTo(TokenKind.Comment);
    }
}
