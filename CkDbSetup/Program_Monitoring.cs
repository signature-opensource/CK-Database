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
        static CommandOption PrepareLogLevelOption( CommandLineApplication c )
        {
            return c.Option( "-l|--logLevel", "Sets a log level filter for console output. Accepts a LogFilter name, or a GroupLogLevelFilter:LineLogLevelFilter combination. Defaults to Info:Info.", CommandOptionType.SingleValue );
        }

        static ActivityMonitor PrepareActivityMonitorFromOptions( CommandOption logLevelOption )
        {
            ActivityMonitor m = new ActivityMonitor();
            ActivityMonitorConsoleClient consoleClient = new ActivityMonitorConsoleClient();
            m.Output.RegisterClient( consoleClient );

            // Default filter
            LogFilter filter = new LogFilter(LogLevelFilter.Info, LogLevelFilter.Info);

            string s = logLevelOption.Value();

            if( !string.IsNullOrWhiteSpace( s ) )
            {
                filter = ParseLogFilterString( logLevelOption.Value() );
            }

            // TODO: Client.Filter { set; } throws an IOE right now
            //consoleClient.Filter = filter;

            return m;
        }

        static LogFilter ParseLogFilterString( string s )
        {
            if( string.IsNullOrWhiteSpace( s ) ) { return LogFilter.Undefined; }
            switch( s.ToLowerInvariant() )
            {
                case "undefined": return LogFilter.Undefined;
                case "off": return LogFilter.Off;
                case "debug": return LogFilter.Debug;
                case "release": return LogFilter.Release;
                case "terse": return LogFilter.Terse;
                case "verbose": return LogFilter.Verbose;
                case "monitor": return LogFilter.Monitor;
            }

            // Allow groupLevel:lineLevel format
            var splitStr = s.Split(':');

            if( splitStr.Length == 2 )
            {
                var groupLevel = ParseLogLevelFilterString(splitStr[0]);
                var lineLevel = ParseLogLevelFilterString(splitStr[1]);

                return new LogFilter( groupLevel, lineLevel );
            }

            return LogFilter.Undefined;
        }

        static LogLevelFilter ParseLogLevelFilterString( string s )
        {
            switch( s.ToLowerInvariant() )
            {
                case "none": return LogLevelFilter.None;
                case "off": return LogLevelFilter.Off;
                case "trace": return LogLevelFilter.Trace;
                case "info": return LogLevelFilter.Info;
                case "warn": return LogLevelFilter.Warn;
                case "error": return LogLevelFilter.Error;
                case "fatal": return LogLevelFilter.Fatal;
            }
            return LogLevelFilter.None;
        }
    }
}
