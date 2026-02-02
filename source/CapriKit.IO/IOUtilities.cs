using System;
using System.Collections.Generic;
using System.Text;

namespace CapriKit.IO;

public static class IOUtilities
{    
    public static StringComparison GetOSPathComparisonType()
    {
        return OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
    }
}
