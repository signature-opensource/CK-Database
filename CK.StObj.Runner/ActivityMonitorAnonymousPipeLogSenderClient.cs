using CK.Core;
using CK.Monitoring;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;

namespace CK.StObj.Runner
{
    class ActivityMonitorAnonymousPipeLogSenderClient : IActivityMonitorClient, IDisposable
    {
        readonly CKBinaryWriter _writer;
        readonly AnonymousPipeClientStream _client;

        public ActivityMonitorAnonymousPipeLogSenderClient( string pipeHandlerName )
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
}
