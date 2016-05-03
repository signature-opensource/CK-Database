using System;
using System.IO;
using System.Text;
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
            return c.Option( "-f|--logFilter", $"Sets a log level filter for console and/or file output. {LogFilterDesc}", CommandOptionType.SingleValue );
        }
        static CommandOption PrepareLogFileOption( CommandLineApplication c )
        {
            return c.Option( "-l|--logFile", $"Path of a log file which will ontain the log output. Defaults to none (console logging only).", CommandOptionType.SingleValue );
        }

        static ActivityMonitor PrepareActivityMonitor( LogFilter lf, string logFilePath )
        {
            ActivityMonitor m = new ActivityMonitor();
            ColoredActivityMonitorConsoleClient consoleClient = new ColoredActivityMonitorConsoleClient();

            m.Output.RegisterClient( consoleClient );

            if( !string.IsNullOrWhiteSpace( logFilePath ) )
            {
                PrepareLogFileWriter( logFilePath, m );
            }

            consoleClient.Filter = lf;

            return m;
        }

        static ActivityMonitor PrepareActivityMonitor( CommandOption logLevelOption, CommandOption logFileOption )
        {
            string filterString = logLevelOption.Value();
            string logFilePath = logFileOption.Value();
            LogFilter lf;

            if( string.IsNullOrWhiteSpace( filterString ) ) { return PrepareActivityMonitor( LogFilter.Undefined, logFilePath ); }

            if( LogFilter.TryParse( filterString, out lf ) )
            {
                return PrepareActivityMonitor( lf, logFilePath );
            }
            else
            {
                return null;
            }
        }

        static ActivityMonitorTextWriterClient LogFileWriterClient;
        static TextWriter LogFileTextWriter;

        static void PrepareLogFileWriter( string logFilePath, IActivityMonitor m )
        {
            if( LogFileTextWriter != null ) { throw new InvalidOperationException(); }
            if( LogFileWriterClient != null ) { throw new InvalidOperationException(); }

            logFilePath = Path.GetFullPath( logFilePath );
            string dir = Path.GetDirectoryName(logFilePath);
            if( !Directory.Exists( dir ) ) { Directory.CreateDirectory( dir ); }

            LogFileTextWriter = new StreamWriter( logFilePath, true, Encoding.UTF8, 4096 );
            LogFileWriterClient = new ActivityMonitorTextWriterClient( ( s ) => LogFileTextWriter.Write( s ) );
            m.Output.RegisterClient( LogFileWriterClient );
        }

        static void DisposeLogFileWriter()
        {
            if( LogFileTextWriter != null )
            {
                LogFileTextWriter.Flush();
                LogFileTextWriter.Dispose();
            }
        }
    }
}
