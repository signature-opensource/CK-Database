using CK.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core.Impl;
using System.IO;
using CK.Monitoring;
using System.Diagnostics;
using CKSetup.Tests;
using System.Threading;

namespace CKSetup.Core.Tests
{
    [TestFixture]
    public class ActivityMonitorAnonymousPipeTests
    {
        class AnonymousPipeLogSender : IActivityMonitorClient, IDisposable
        {
            readonly CKBinaryWriter _writer;
            readonly AnonymousPipeClientStream _client;

            public AnonymousPipeLogSender( string pipeHandlerName )
            {
                _client = new AnonymousPipeClientStream( PipeDirection.Out, pipeHandlerName );
                _writer = new CKBinaryWriter( _client );
                _writer.Write( LogReader.CurrentStreamVersion );
            }

            public void Dispose()
            {
                _client.WriteByte( 0 );
                _client.WaitForPipeDrain();
                _writer.Dispose();
                _client.Dispose();
            }

            void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
            {
            }

            void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
            {
                LogEntry.WriteCloseGroup( _writer, group.GroupLevel, group.CloseLogTime, conclusions );
            }

            void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion> conclusions )
            {
            }

            void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
            {
                LogEntry.WriteLog( _writer, true, group.GroupLevel, group.LogTime, group.GroupText, group.GroupTags, group.ExceptionData, group.FileName, group.LineNumber );
            }

            void IActivityMonitorClient.OnTopicChanged( string newTopic, string fileName, int lineNumber )
            {
            }

            void IActivityMonitorClient.OnUnfilteredLog( ActivityMonitorLogData data )
            {
                LogEntry.WriteLog( _writer, false, data.Level, data.LogTime, data.Text, data.Tags, data.ExceptionData, data.FileName, data.LineNumber );
            }
        }


        [Test]
        public void sending_log_from_client()
        {
            var m = new ActivityMonitor();
            using( m.Output.CreateBridgeTo( TestHelper.Monitor.Output.BridgeTarget ) )
            {
                string pipeHandlerName = LogReceiver.Start( m );
                RunClient( pipeHandlerName );
            }
        }

        class LogReceiver
        {
            readonly AnonymousPipeServerStream _server;
            readonly CKBinaryReader _reader;
            readonly IActivityMonitor _monitor;

            LogReceiver( AnonymousPipeServerStream s, IActivityMonitor m )
            {
                _server = s;
                _reader = new CKBinaryReader( _server );
                _monitor = m;
            }

            void Run( object unused )
            {
                int streamVersion = _reader.ReadInt32();
                for( ; ; )
                {
                    var e = LogEntry.Read( _reader, streamVersion, out bool badEndOfStream );
                    if( e == null || badEndOfStream ) break;
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

            public static string Start( IActivityMonitor m )
            {
                var server = new AnonymousPipeServerStream( PipeDirection.In/*, HandleInheritability.Inheritable*/ );
                var pipeName = server.GetClientHandleAsString();
                //server.DisposeLocalCopyOfClientHandle();
                var r = new LogReceiver( server, m );
                ThreadPool.QueueUserWorkItem( r.Run );
                return pipeName;
            }
        }

        void RunClient( string pipeHandlerName )
        {
            var m = new ActivityMonitor( false );
            using( var pipe = new AnonymousPipeLogSender( pipeHandlerName ) )
            {
                m.Output.RegisterClient( pipe );
                try
                {
                    using( m.OpenInfo( "From client." ) )
                    {
                        m.Fatal( "A fatal.", new Exception( "An Exception for the fun." ) );
                    }
                }
                catch( Exception ex )
                {
                    m.Fatal( ex );
                }
            }
        }
    }
}
