using CapriKit.AssetPipeline;
using CapriKit.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace CapriKit.Tests.AssetPipeline;

internal class HotReloadManagerTests
{
    [Test]
    public async Task Foo()
    {
        var fileSystem = new InMemoryFileSystem().ScopedTo("C:/Test");
        var sut = new HotReloadManager(NullLoggerFactory.Instance, fileSystem);
    }
}
