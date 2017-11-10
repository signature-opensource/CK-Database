using CK.Core;
using CK.Monitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;

namespace CKSetup
{
    class LogReceiver : IDisposable
    {
        readonly AnonymousPipeServerStream _server;
        readonly CKBinaryReader _reader;
        readonly IActivityMonitor _monitor;
        readonly Thread _thread;
        readonly bool _interProcess;
        LogReceiverEndStatus _endFlag;

        LogReceiver( IActivityMonitor m, bool interProcess )
        {
            _interProcess = interProcess;
            var inherit = interProcess ? HandleInheritability.Inheritable : HandleInheritability.None;
            _server = new AnonymousPipeServerStream( PipeDirection.In, inherit );
            _reader = new CKBinaryReader( _server );
            _monitor = m;
            PipeName = _server.GetClientHandleAsString();
            _thread = new Thread( Run );
            _thread.IsBackground = true;
            _thread.Start();
        }

        public string PipeName { get; }

        public LogReceiverEndStatus WaitEnd()
        {
            _thread.Join();
            return _endFlag;
        }

        public void Dispose()
        {
            _thread.Join();
            _reader.Dispose();
            _server.Dispose();
        }

        void Run()
        {
            try
            {
                int streamVersion = _reader.ReadInt32();
                if( _interProcess ) _server.DisposeLocalCopyOfClientHandle();
                for(; ; )
                {
                    var e = LogEntry.Read( _reader, streamVersion, out bool badEndOfStream );
                    if( e == null || badEndOfStream )
                    {
                        _endFlag = badEndOfStream ? LogReceiverEndStatus.MissingEndMarker : LogReceiverEndStatus.Normal;
                        break;
                    }
                    switch( e.LogType )
                    {
                        case LogEntryType.Line:
                            _monitor.UnfilteredLog( e.Tags, e.LogLevel, e.Text, e.LogTime, CKException.CreateFrom( e.Exception ), e.FileName, e.LineNumber );
                            break;
                        case LogEntryType.OpenGroup:
                            _monitor.UnfilteredOpenGroup( e.Tags, e.LogLevel, null, e.Text, e.LogTime, CKException.CreateFrom( e.Exception ), e.FileName, e.LineNumber );
                            break;
                        case LogEntryType.CloseGroup:
                            _monitor.CloseGroup( e.LogTime, e.Conclusions );
                            break;
                    }
                }
            }
            catch( Exception ex )
            {
                _endFlag = LogReceiverEndStatus.Error;
                ActivityMonitor.CriticalErrorCollector.Add( ex, "While LogReceiver.Run." );
            }
        }

        public static LogReceiver Start( IActivityMonitor m, bool interProcess ) => new LogReceiver( m, interProcess );

    }

}
