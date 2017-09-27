using CK.Core;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CKSetup
{

    public class ConsoleMonitor : IActivityMonitor, IDisposable
    {
        readonly IActivityMonitor _m;
        readonly CommandLineApplication _c;
        StreamWriter _fileWriter;

        public ConsoleMonitor( CommandLineApplication c, CommandOption logFileOption = null )
        {
            _c = c;
            _m = new ActivityMonitor();
            var consoleClient = new ColoredActivityMonitorConsoleClient();
            _m.Output.RegisterClient( consoleClient );
            if( logFileOption != null && !string.IsNullOrWhiteSpace( logFileOption.Value() ) )
            {
                try
                {
                    var logFilePath = Path.GetFullPath( logFileOption.Value() );
                    string dir = Path.GetDirectoryName( logFilePath );
                    Directory.CreateDirectory( dir );

                    _fileWriter = new StreamWriter( logFilePath, true, Encoding.UTF8, 4096 );
                    _m.Output.RegisterClient( new ActivityMonitorTextWriterClient( _fileWriter.Write ) );
                }
                catch( Exception ex )
                {
                    _m.Warn( "While creating log file.", ex );
                }
            }
        }

        public CKTrait AutoTags { get => _m.AutoTags; set => _m.AutoTags = value; }

        public LogFilter MinimalFilter { get => _m.MinimalFilter; set => _m.MinimalFilter = value; }

        public LogFilter ActualFilter => _m.ActualFilter;

        public string Topic => _m.Topic;

        public IActivityMonitorOutput Output => _m.Output;

        public DateTimeStamp LastLogTime => _m.LastLogTime;

        public bool CloseGroup( DateTimeStamp logTime, object userConclusion = null ) => _m.CloseGroup( logTime, userConclusion );

        public void Dispose()
        {
            _m.MonitorEnd();
            if( _fileWriter != null )
            {
                _fileWriter.Flush();
                _fileWriter.Dispose();
                _fileWriter = null;
            }
        }

        public void SetTopic( string newTopic, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNumber = 0 )
            => _m.SetTopic( newTopic, fileName, lineNumber );

        public void UnfilteredLog( ActivityMonitorLogData data ) => _m.UnfilteredLog( data );

        public IDisposableGroup UnfilteredOpenGroup( ActivityMonitorGroupData data ) => _m.UnfilteredOpenGroup( data );

        public int SendErrorAndDisplayHelp( string msg )
        {
            _m.Error( msg );
            _c.ShowHelp();
            return Program.RetCodeError;
        }

        public int SendError( string msg )
        {
            _m.Error( msg );
            return Program.RetCodeError;
        }
    }

}
