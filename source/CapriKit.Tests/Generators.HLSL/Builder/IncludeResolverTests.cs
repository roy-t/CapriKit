using CapriKit.Generators.HLSL.Builder;
using CapriKit.Generators.HLSL.Parser;

namespace CapriKit.Tests.Generators.HLSL.Builder;

internal class IncludeResolverTests
{
    [Test]
    public async Task GetIncludedFilesReturnsTransitiveLocalIncludesInDependencyOrder()
    {
        var a = Shader(Local("b.hlsl"));
        var b = Shader(Local("c.hlsl"), new Include("system.hlsl", IncludeKind.System));
        var c = Shader();

        var resolver = new IncludeResolver(
        [
            ("C:/shaders/a.hlsl", a),
            ("C:/shaders/b.hlsl", b),
            ("C:/shaders/c.hlsl", c),
        ]);

        var result = resolver.GetIncludedFiles("C:/shaders/a.hlsl", a);

        // c is included by b, so it must be resolved first; the system include is ignored.
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(Path.GetFileName(result[0].Item1)).IsEqualTo("c.hlsl");
        await Assert.That(Path.GetFileName(result[1].Item1)).IsEqualTo("b.hlsl");
    }

    [Test]
    public async Task GetIncludedFilesResolvesADiamondOnce()
    {
        var a = Shader(Local("b.hlsl"), Local("c.hlsl"));
        var b = Shader(Local("d.hlsl"));
        var c = Shader(Local("d.hlsl"));
        var d = Shader();

        var resolver = new IncludeResolver(
        [
            ("C:/shaders/a.hlsl", a),
            ("C:/shaders/b.hlsl", b),
            ("C:/shaders/c.hlsl", c),
            ("C:/shaders/d.hlsl", d),
        ]);

        var result = resolver.GetIncludedFiles("C:/shaders/a.hlsl", a);

        // d is reached through both b and c but must appear once, before its dependents.
        await Assert.That(result.Count).IsEqualTo(3);
        await Assert.That(Path.GetFileName(result[0].Item1)).IsEqualTo("d.hlsl");
    }

    private static ShaderMetadata Shader(params Include[] includes) => new(includes, [], [], [], []);

    private static Include Local(string path) => new(path, IncludeKind.Local);
}
