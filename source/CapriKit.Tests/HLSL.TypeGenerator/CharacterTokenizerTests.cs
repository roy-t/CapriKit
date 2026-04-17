using CapriKit.HLSL.TypeGenerator.Tokenizer;

namespace CapriKit.Tests.HLSL.TypeGenerator;

internal class CharacterTokenizerTests
{

    // Offsets:                                 1    6     12      20      28     35      43   48
    private static readonly string Source = """['a', '\n', '\101', '\x51', 'not', '\not', "n", '\101a']""";

    [Test]
    public async Task ReadCharacter()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 1, tokens);
        await Assert.That(read).IsEqualTo(3);
        await Assert.That(tokens).IsNotEmpty();
        await Assert.That(tokens[0].Value).EqualTo("'a'");
    }

    [Test]
    public async Task ReadCharacter_EscapedCharacter()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 6, tokens);
        await Assert.That(read).IsEqualTo(4);
        await Assert.That(tokens).IsNotEmpty();
        await Assert.That(tokens[0].Value).EqualTo(@"'\n'");
    }

    [Test]
    public async Task ReadCharacter_OctalEscapeSequence()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 12, tokens);
        await Assert.That(read).IsEqualTo(6);
        await Assert.That(tokens).IsNotEmpty();
        await Assert.That(tokens[0].Value).EqualTo(@"'\101'");
    }

    [Test]
    public async Task ReadCharacter_HexEscapeSequence()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 20, tokens);
        await Assert.That(read).IsEqualTo(6);
        await Assert.That(tokens).IsNotEmpty();
        await Assert.That(tokens[0].Value).EqualTo(@"'\x51'");
    }

    [Test]
    public async Task ReadCharacter_IllegalCharSequence()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 28, tokens);
        await Assert.That(read).IsEqualTo(0);
        await Assert.That(tokens).IsEmpty();
    }

    [Test]
    public async Task ReadCharacter_NotACharLiteral()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 43, tokens);
        await Assert.That(read).IsEqualTo(0);
        await Assert.That(tokens).IsEmpty();
    }

    [Test]
    public async Task ReadCharacter_NotAnOctalLiteral()
    {
        var tokens = new List<Token>();
        var read = CharacterTokenizer.ReadCharacter(Source, 48, tokens);
        await Assert.That(read).IsEqualTo(0);
        await Assert.That(tokens).IsEmpty();
    }
}
