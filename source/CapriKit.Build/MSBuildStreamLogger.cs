using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace CapriKit.Build;

internal sealed class MSBuildStreamLogger : Logger
{
    private readonly TextWriter stream;

    public MSBuildStreamLogger(TextWriter stream)
    {
        this.stream = TextWriter.Synchronized(stream);
    }

    public override void Initialize(IEventSource eventSource)
    {
        eventSource.AnyEventRaised += (_, args) =>
        {
            this.stream.WriteLine(args.Message);
        };
    }
}
