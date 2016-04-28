using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Core.Impl;

namespace CkDbSetup
{
    class StupidFlatActivityMonitorConsoleClient : IActivityMonitorBoundClient
    {
        TextWriter _out;
        bool _noColor;
        IActivityMonitorImpl _source;
        LogFilter _filter;

        string _lineHeader;

        Stack<IActivityLogGroup> _openGroups;

        public LogFilter MinimalFilter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public StupidFlatActivityMonitorConsoleClient( LogFilter filter, bool useErrorOutput = false, bool noColor = false )
        {
            _out = useErrorOutput ? System.Console.Error : System.Console.Out;
            _noColor = noColor;
            _lineHeader = String.Empty;
            _filter = filter;

            _openGroups = new Stack<IActivityLogGroup>();
        }

        public StupidFlatActivityMonitorConsoleClient( bool useErrorOutput = false, bool noColor = false ) : this( LogFilter.Undefined, useErrorOutput, noColor )
        {
        }

        void SetColor( LogLevel l )
        {
            if( _noColor ) { return; }

            switch( l )
            {
                case LogLevel.Fatal:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warn:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogLevel.Trace:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
        }

        public void OnAutoTagsChanged( CKTrait newTrait )
        {
            SetColor( LogLevel.None );
            WriteLineHeader();
            _out.WriteLine( "[AutoTags] {0}", newTrait );
        }

        public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            if( CanOutputGroup( group.MaskedGroupLevel ) )
            {
                SetColor( LogLevel.None );
                foreach( var c in conclusions )
                {
                    WriteLineHeader();
                    _out.WriteLine( "[Conclusion] {0}", c.Text );
                }
            }
            if( _openGroups.Count > 0 ) { _openGroups.Pop(); }
            RecalculateLineHeader();
            Console.ResetColor();
        }

        public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
        {
        }

        public void OnOpenGroup( IActivityLogGroup group )
        {
            _openGroups.Push( group );
            RecalculateLineHeader();
            if( CanOutputGroup( group.MaskedGroupLevel ) )
            {
                SetColor( group.MaskedGroupLevel );
                WriteLineHeader();
                _out.WriteLine( "[Group] {0}", group.GroupText );
                WriteException( group.ExceptionData );
            }
            Console.ResetColor();
        }

        public void OnTopicChanged( string newTopic, string fileName, int lineNumber )
        {
            SetColor( LogLevel.None );
            WriteLineHeader();
            _out.WriteLine( "[Topic] {0}", newTopic );
            Console.ResetColor();
        }

        public void OnUnfilteredLog( ActivityMonitorLogData data )
        {
            if( CanOutputLine( data.MaskedLevel ) )
            {
                SetColor( data.MaskedLevel );
                WriteLineHeader();
                _out.WriteLine( "{0}", data.Text );
                WriteException( data.ExceptionData );
            }
            Console.ResetColor();
        }

        void RecalculateLineHeader()
        {
            int groupLevel = 0;
            if( _openGroups.Count > 0 )
            {
                groupLevel = _openGroups.Peek().Depth;
            }

            StringBuilder sb = new StringBuilder();

            for( int i = 0; i < groupLevel; i++ )
            {
                sb.Append( '>' );
            }

            if( groupLevel > 0 )
            {
                sb.Append( ' ' );
            }

            _lineHeader = sb.ToString();
        }
        void WriteLineHeader()
        {
            _out.Write( _lineHeader );
        }

        void WriteException( CKExceptionData e )
        {
            if( e != null )
            {
                _out.WriteLine( e.ToString() );
            }
        }

        public LogFilter Filter
        {
            get { return _filter; }
            set
            {
                LogFilter oldFilter = _filter;
                _filter = value;
                if( _source != null ) _source.OnClientMinimalFilterChanged( oldFilter, _filter );
            }
        }

        LogFilter IActivityMonitorBoundClient.MinimalFilter
        {
            get { return _filter; }
        }

        void IActivityMonitorBoundClient.SetMonitor( IActivityMonitorImpl source, bool forceBuggyRemove )
        {
            if( !forceBuggyRemove )
            {
                if( source != null && _source != null ) throw ActivityMonitorClient.CreateMultipleRegisterOnBoundClientException( this );
            }
            _openGroups.Clear();
            _source = source;
        }

        bool CanOutputLine( LogLevel logLevel )
        {
            Debug.Assert( (logLevel & LogLevel.IsFiltered) == 0, "The level must already be masked." );
            return _filter.Line == LogLevelFilter.None || (int)logLevel >= (int)_filter.Line;
        }

        bool CanOutputGroup( LogLevel logLevel )
        {
            Debug.Assert( (logLevel & LogLevel.IsFiltered) == 0, "The level must already be masked." );
            return _filter.Group == LogLevelFilter.None || (int)logLevel >= (int)_filter.Group;
        }
    }
}
