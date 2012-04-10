using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CK.Database.SqlServer;
using CK.Core;

namespace CK.Setup.Database.SqlServer
{
    public class DatabaseExecutor : IDatabaseExecutor, IDisposable
    {
        StringWriter _buffer;
        SqlManager _manager;
        IActivityLogger _logger;
        
        public DatabaseExecutor( string connectionString, IActivityLogger logger )
        {
            if( String.IsNullOrWhiteSpace( connectionString ) ) throw new ArgumentException( "connectionString" );
            if( logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _manager = new SqlManager();
            _manager.Logger = logger;
            _manager.OpenFromConnectionString( connectionString );
            _buffer = new StringWriter();
        }

        public SqlConnectionProvider Connection 
        {
            get { return _manager.Connection; } 
        }

        public bool ExecuteScript( string script )
        {
            return _manager.ExecScript( script );
        }

        public bool ExecuteScriptNoLog( string script )
        {
            return _manager.ExecScriptLog( script, null );
        }

        public bool ExecuteScript( params Action<TextWriter>[] writers )
        {
            foreach( var f in writers )
            {
                _buffer.GetStringBuilder().Length = 0;
                f( _buffer );
                if( !ExecuteScript( _buffer.GetStringBuilder().ToString() ) ) return false;
            }
            return true;
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

    }
}
