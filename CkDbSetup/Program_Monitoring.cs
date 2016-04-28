using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using Microsoft.Extensions.CommandLineUtils;

namespace CkDbSetup
{
    static partial class Program
    {
        static readonly string LogFilterDesc = "Valid log filters: \"Off\", \"Release\", \"Monitor\", \"Terse\", \"Verbose\", \"Debug\", or any \"{Group,Line}\" format where Group and Line can be: \"Trace\", \"Info\", \"Warn\", \"Error\", \"Fatal\", or \"Off\".";
        static readonly string LogFilterErrorDesc = $"\nError: The given log filter is not valid. {LogFilterDesc}";

        static CommandOption PrepareLogLevelOption( CommandLineApplication c )
        {
            return c.Option( "-l|--logLevel", $"Sets a log level filter for console output. Defaults to {{Info,Info}}. {LogFilterDesc}", CommandOptionType.SingleValue );
        }

        static ActivityMonitor PrepareActivityMonitor( LogFilter lf )
        {
            ActivityMonitor m = new ActivityMonitor();
            StupidFlatActivityMonitorConsoleClient consoleClient = new StupidFlatActivityMonitorConsoleClient();
            m.Output.RegisterClient( consoleClient );

            // TODO: Client.Filter { set; } throws an IOE right now
            consoleClient.Filter = lf;

            return m;
        }

        static ActivityMonitor PrepareActivityMonitor( CommandOption logLevelOption )
        {
            string s = logLevelOption.Value();
            LogFilter lf;

            if( string.IsNullOrWhiteSpace( s ) ) { return PrepareActivityMonitor( LogFilter.Undefined ); }

            if( LogFilter.TryParse( s, out lf ) )
            {
                return PrepareActivityMonitor( lf );
            }
            else
            {
                return null;
            }
        }
    }
}
