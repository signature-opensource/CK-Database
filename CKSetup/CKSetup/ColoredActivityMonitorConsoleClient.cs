using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Core.Impl;

namespace CKSetup
{
    /// <summary>
    /// Colored console client 
    /// </summary>
    class ColoredActivityMonitorConsoleClient : ActivityMonitorTextWriterClient
    {
        readonly TextWriter _out;
        readonly bool _noColor;

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with a filter initially set.
        /// </summary>
        /// <param name="filter"><see cref="LogFilter"/> to set on this monitor</param>
        /// <param name="useErrorStream">When true, the client output will be sent to the error stream (stderr). Otherwise, output will be sent to the standard stream (stdout).</param>
        /// <param name="noColor">When true, the background and foreground console colors will not be changed.</param>
        public ColoredActivityMonitorConsoleClient( LogFilter filter, bool useErrorStream = false, bool noColor = false )
            : base( GetWriter(useErrorStream), filter )
        {
            _out = useErrorStream ? System.Console.Error : System.Console.Out;
            _noColor = noColor;
        }

        static Action<string> GetWriter( bool useErrorStream )
        {
            if( useErrorStream ) return s => Console.Error.Write( s );
            return s => Console.Out.Write( s );
        }

        public ColoredActivityMonitorConsoleClient( bool useErrorOutput = false, bool noColor = false )
            : this( LogFilter.Undefined, useErrorOutput, noColor )
        {
        }

        void SetColor( LogLevel l )
        {
            if( _noColor ) { return; }

            Console.ResetColor();
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

        protected override void OnEnterLevel( ActivityMonitorLogData data )
        {
            SetColor( data.MaskedLevel );
            base.OnEnterLevel( data );
        }

        protected override void OnLeaveLevel( LogLevel level )
        {
            base.OnLeaveLevel( level );
            Console.ResetColor();
        }

        protected override void OnGroupOpen( IActivityLogGroup g )
        {
            SetColor( g.MaskedGroupLevel );
            base.OnGroupOpen( g );
        }

        protected override void OnGroupClose( IActivityLogGroup g, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            SetColor( g.MaskedGroupLevel );
            base.OnGroupClose( g, conclusions );
            Console.ResetColor();
        }
    }
}
