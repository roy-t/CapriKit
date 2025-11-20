using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapriKit.Build;

internal class MSBuildFileLogger : Microsoft.Build.Utilities.Logger
{
    private StreamWriter? logFileWriter = null;
    
    public override void Initialize(IEventSource eventSource)
    {
        var file = Path.GetTempFileName();
        var stream = File.OpenWrite(file);

        logFileWriter = new StreamWriter(stream);

        eventSource.AnyEventRaised += LogEvent;

        throw new NotImplementedException();

        // TODO: when to close stream?
    }

    private void LogEvent(object _, BuildEventArgs args)
    {
        if(!string.IsNullOrEmpty(args.Message))
        {
            logFileWriter?.WriteLine(args.Message);
        }        
    }
}
