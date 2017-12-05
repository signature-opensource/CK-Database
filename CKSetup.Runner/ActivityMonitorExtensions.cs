using CK.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CKSetup.Runner
{
    static class ActivityMonitorExtensions
    {
        static public void Log( this IActivityMonitor @this, LogLevel level, string text, Exception ex = null, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            @this.UnfilteredLog( null, level, text, @this.NextLogTime(), ex, fileName, lineNumber );
        }
        static public IDisposable OpenLog( this IActivityMonitor @this, LogLevel level, string text, Exception ex = null, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            return @this.UnfilteredOpenGroup( null, level, null, text, @this.NextLogTime(), ex, fileName, lineNumber );
        }
    }
}
