using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Offers script execution facility and higher level database management (such as automatically 
    /// creating a database) for Sql server databases.
    /// </summary>
    public class SqlManager : ISqlManager
    {
        static List<string> _protectedDatabaseNames = new List<string>() { "master", "msdb", "tempdb", "model" };

        readonly IActivityMonitor   _monitor;
        SqlConnection               _oCon;
        bool				    	_checkTranCount;
        bool                        _ckCoreInstalled;
        bool                        _missingDependencyIsError;
        bool                        _ignoreMissingDependencyIsError;

        /// <summary>
        /// Initializes a new SqlManager.
        /// </summary>
        /// <param name="monitor">Required non null monitor.</param>
        public SqlManager( IActivityMonitor monitor )
        {
            _monitor = monitor ?? throw new ArgumentNullException( nameof( monitor ) );
            _checkTranCount = true;
        }

        /// <summary>
        /// Gets the <see cref="SqlConnection"/> of this manager.
        /// </summary>
        public SqlConnection Connection => _oCon;

        void IDisposable.Dispose() => Close();

        /// <summary>
        /// Close the connection. <see cref="Connection"/> becomes null.
        /// Can be called multiple times.
        /// </summary>
        public void Close()
        {
            if( _oCon != null )
            {
                _oCon.StateChange -= new StateChangeEventHandler( OnConnStateChange );
                _oCon.InfoMessage -= new SqlInfoMessageEventHandler( OnConnInfo );
                _oCon.Dispose();
                _oCon = null;
            }
        }

        void DoOpen( string connectionString, bool clearPoolFirst = false )
        {
            Debug.Assert( _oCon == null );
            try
            {
                _oCon = new SqlConnection( connectionString );
                CheckAction( "opening", _oCon.Database );
                if( clearPoolFirst ) SqlConnection.ClearPool( _oCon );
                if( _monitor != null )
                {
                    _oCon.StateChange += new StateChangeEventHandler( OnConnStateChange );
                    _oCon.InfoMessage += new SqlInfoMessageEventHandler( OnConnInfo );
                }
                _oCon.Open();
            }
            catch
            {
                Close();
                throw;
            }
        }

        void CheckOpen()
        {
            if( _oCon == null ) throw new InvalidOperationException( "SqlManager is closed." );
        }

        /// <summary>
        /// Gets or sets whether transaction count must be equal before and after 
        /// executing scripts. Defaults to true.
        /// </summary>
        bool CheckTransactionCount
        {
            get { return _checkTranCount; }
            set { _checkTranCount = value; }
        }

        /// <summary>
        /// Gets or sets whether whenever a creation script is executed, the informational message
        /// 'The module 'X' depends on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="CK.Core.LogLevel.Error"/>. When false, this is a <see cref="CK.Core.LogLevel.Info"/>.
        /// Defaults to false.
        /// Note that if <see cref="IgnoreMissingDependencyIsError"/> is true, this property has no effect and a missing dependency will remain informational.
        /// </summary>
        public bool MissingDependencyIsError
        {
            get { return _missingDependencyIsError; }
            set { _missingDependencyIsError = value; }
        }

        /// <summary>
        /// Gets or sets whether <see cref="MissingDependencyIsError"/> must be ignored.
        /// When true, MissingDependencyIsError is always considered to be false.
        /// Defaults to true (MissingDependencyIsError is honored).
        /// </summary>
        public bool IgnoreMissingDependencyIsError
        {
            get { return _ignoreMissingDependencyIsError; }
            set { _ignoreMissingDependencyIsError = value; }
        }

        /// <summary>
        /// Opens a database from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="autoCreate">False to not creating the database if it does not exist.</param>
        /// <returns>True on success.</returns>
        public bool OpenFromConnectionString( string connectionString, bool autoCreate = false )
        {
            using( _monitor.OpenInfo( $"Connection to {connectionString}." ) )
            {
                try
                {
                    Close();
                    DoOpen( connectionString );
                    return true;
                }
                catch( Exception ex )
                {
                    if( autoCreate )
                    {
                        _monitor.Warn( ex );
                        try
                        {
                            string name;
                            using( var master = new SqlConnection( GetMasterConnectionString( connectionString, out name ) ) )
                            {
                                _monitor.Info( $"Creating database '{name}'." );
                                master.Open();
                                using( var cmd = new SqlCommand( $"create database [{name.Replace( "[", "[[" ).Replace( "]", "]]" )}]" ) { Connection = master } ) cmd.ExecuteNonQuery();
                            }
                        }
                        catch( Exception exCreate )
                        {
                            _monitor.Error( exCreate );
                            return false;
                        }
                        try
                        {
                            DoOpen( connectionString, true );
                            return true;
                        }
                        catch( Exception exOpenCreated )
                        {
                            _monitor.Error( exOpenCreated );
                            return false;
                        }
                    }
                    else
                    {
                        _monitor.Error( ex );
                        return false;
                    }
                }
            }
        }


        /// <summary>
        /// Small helper that opens or crates a database and returns an opened <see cref="SqlManager"/>.
        /// </summary>
        /// <param name="connectionString">Connection string to use.</param>
        /// <param name="monitor">Monitor that will be associated to the SqlManager. Can not be null.</param>
        /// <returns>Opened SqlManager.</returns>
        static public SqlManager OpenOrCreate( string connectionString, IActivityMonitor monitor )
        {
            SqlManager m = new SqlManager( monitor );
            m.OpenFromConnectionString( connectionString, true );
            return m;
        }

        /// <summary>
        /// Gets the <see cref="IActivityMonitor"/>.
        /// </summary>
        public IActivityMonitor Monitor => _monitor; 

        /// <summary>
        /// Ensures that the CKCore kernel is installed.
        /// </summary>
        /// <param name="monitor">The monitor to use. Can not be null.</param>
        /// <returns>True on success.</returns>
        public bool EnsureCKCoreIsInstalled( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            CheckOpen();
            if( !_ckCoreInstalled )
            {
                _ckCoreInstalled = SqlCKCoreInstaller.Install( this, monitor );
            }
            return _ckCoreInstalled;
        }

        /// <summary>
        /// Returns the object text definition of <paramref name="schemaName"/> object.
        /// </summary>
        /// <param name="schemaName">Namme of the object.</param>
        /// <returns>The object's text.</returns>
        public string GetObjectDefinition( string schemaName )
        {
            CheckOpen();
            using( var cmd = new SqlCommand( "select OBJECT_DEFINITION(OBJECT_ID(@0))" ) { Connection = _oCon } )
            {
                cmd.Parameters.AddWithValue( "@0", schemaName );
                return (string)cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schema">Name of the schema. Must not be null nor empty.</param>
        /// <param name="dropSchema">True to drop the schema itself.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool SchemaDropAllObjects( string schema, bool dropSchema )
        {
            CheckOpen();
            if( string.IsNullOrEmpty( schema )
                || schema.IndexOf( '\'' ) >= 0
                || schema.IndexOf( ';' ) >= 0 ) throw new ArgumentException( "schemaName" );
            try
            {
                using( var c = new SqlCommand( "CKCore.sSchemaDropAllObjects" ) { Connection = _oCon } )
                {
                    c.CommandType = CommandType.StoredProcedure;
                    c.Parameters.AddWithValue( "@SchemaName", schema );
                    c.ExecuteNonQuery();
                    if( dropSchema )
                    {
                        c.CommandType = CommandType.Text;
                        c.CommandText = $"if exists(select 1 from sys.schemas where name = '{schema}') drop schema [{schema.Replace( "]", "]]" )}];";
                        c.ExecuteNonQuery();
                    }
                }
                if( schema == "CKCore" ) _ckCoreInstalled = false;
                return true;
            }
            catch( Exception ex )
            {
                _monitor.Error( ex );
                return false;
            }
        }

        void CheckAction( string action, string dbName )
        {
            if( dbName == null || dbName.Length == 0 || _protectedDatabaseNames.Contains( dbName ) )
            {
                throw new Exception( $"Attempt to {action} database '{dbName}'." );
            }
        }

        class SqlExecutor : ISqlScriptExecutor
        {
            readonly SqlManager _manager;
            readonly SqlCommand _command;
            readonly IActivityMonitor _monitor;
            readonly int _tranCount;
            readonly string _databaseName;

            /// <summary>
            /// Gets or sets the number of <see cref="Execute"/> that failed.
            /// </summary>
            public int FailCount { get; set; }

            /// <summary>
            /// Gets whether the last <see cref="Execute"/> succeed.
            /// </summary>
            public bool LastSucceed { get; private set; }

            internal SqlExecutor( SqlManager m, IActivityMonitor monitor, bool checkTransactionCount, bool autoRestoreDatabase )
            {
                _manager = m;
                _monitor = monitor;
                _command = new SqlCommand();
                // 12 hours timeout... should be enough!
                _command.CommandTimeout = 18 * 60 * 60;
                _command.Connection = _manager.Connection;
                _databaseName = autoRestoreDatabase ? _command.Connection.Database : null;
                if( checkTransactionCount )
                {
                    _command.CommandText = "select @@TranCount;";
                    _tranCount = (int)_command.ExecuteScalar();
                }
                else _tranCount = -1;
            }

            public bool Execute( string script )
            {
                if( script == null ) throw new ArgumentNullException( "script" );
                LastSucceed = false;
                bool hasBeenTraced = false;
                try
                {
                    script = script.Trim();
                    if( script.Length > 0 )
                    {
                        _command.CommandText = script;
                        if( _monitor != null )
                        {
                            hasBeenTraced = _monitor.ShouldLogLine( LogLevel.Trace );
                            if( hasBeenTraced )
                            {
                                _monitor.UnfilteredLog( ActivityMonitor.Tags.Empty, LogLevel.Trace | LogLevel.IsFiltered, script, _monitor.NextLogTime(), null );
                                _monitor.UnfilteredLog( ActivityMonitor.Tags.Empty, LogLevel.Trace | LogLevel.IsFiltered, "GO", _monitor.NextLogTime(), null );
                            }
                        }
                        _command.ExecuteNonQuery();
                    }
                    LastSucceed = true;
                }
                catch( Exception e )
                {
                    FailCount = FailCount + 1;
                    if( _monitor == null ) throw;
                    // If the monitor is tracing, the text has already been logged.
                    if( hasBeenTraced ) _monitor.Error( e );
                    else
                    {
                        // If the text is not already logged, then we unconditionally log it below the error.
                        using( _monitor.OpenError( e ) )
                        {
                            _monitor.Info( script );
                        }
                    }
                }
                return LastSucceed;
            }

            public void Dispose()
            {
                _command.Dispose();
                try
                {
                    if( _tranCount >= 0 )
                    {
                        int tranCountAfter = (int)_manager.ExecuteScalar( "select @@TranCount" );
                        if( _tranCount != tranCountAfter )
                        {
                            string msg = $"Transaction count differ: {_tranCount} before, {tranCountAfter} after.";
                            int nbRollbak = tranCountAfter - _tranCount;
                            if( _tranCount == 0 && nbRollbak > 0 )
                            {
                                msg += " Attempting rollback: ";
                                try
                                {
                                    _manager.ExecuteNonQuery( "rollback" );
                                    msg += "Succeed.";
                                }
                                catch( Exception ex )
                                {
                                    msg += "Failed -> " + ex.Message;
                                }
                            }
                            if( _monitor != null ) _monitor.Error( msg );
                            else if( LastSucceed ) throw new Exception( msg );
                        }
                    }
                    if( _databaseName != null && _databaseName != _manager.Connection.Database )
                    {
                        if( _monitor != null ) _monitor.Info( $"Current database automatically restored from {_manager.Connection.Database} to {_databaseName}." );
                        _command.Connection.ChangeDatabase( _databaseName );
                    }
                }
                catch( Exception ex )
                {
                    if( _monitor != null ) _monitor.OpenWarn( ex );
                    else
                    {
                        if( LastSucceed ) throw;
                        // When an error already occurred, we do not rethrow the internal exception.
                    }
                }
            }
        }

        /// <summary>
        /// The script is traced (if <paramref name="monitor"/> is not null).
        /// </summary>
        /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
        /// <param name="checkTransactionCount">By default, transaction count is checked: it must be the same before and after the execution.</param>
        /// <param name="autoRestoreDatabase">By default, if the script USE another database, the initial one is automatically restored.</param>
        public ISqlScriptExecutor CreateExecutor( IActivityMonitor monitor, bool checkTransactionCount = true, bool autoRestoreDatabase = true )
        {
            CheckOpen();
            return new SqlExecutor( this, monitor, checkTransactionCount, autoRestoreDatabase );
        }

        /// <summary>
        /// Simple helper to call <see cref="ExecuteOneScript"/> for multiple scripts (this uses the same <see cref="ISqlScriptExecutor"/>).
        /// </summary>
        /// <param name="scripts">Set of scripts to execute.</param>
        /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
        /// <returns>
        /// Always true if <paramref name="monitor"/> is null since otherwise an exception
        /// will be thrown in case of failure. 
        /// If a monitor is set, this method will return true or false to indicate success.
        /// </returns>
        public bool ExecuteScripts( IEnumerable<string> scripts, IActivityMonitor monitor )
        {
            using( var e = CreateExecutor( monitor ) )
            {
                return e.Execute( scripts ) == 0;
            }
        }

        /// <summary>
        /// Executes one script (no GO separator must exist inside). 
        /// The script is traced (if <paramref name="monitor"/> is not null).
        /// </summary>
        /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>
        /// Always true if <paramref name="monitor"/> is null since otherwise an exception
        /// will be thrown in case of failure. 
        /// If a monitor is set, this method will return true or false to indicate success.
        /// </returns>
        /// <remarks>
        /// At the end of the execution, the current database is checked and if it has changed,
        /// the connection is automatically restored onto the original database.
        /// This behavior enables the use of <code>Use OtherDbName</code> commands from inside 
        /// any script and guaranty that, at the beginning of a script, we always are on the 
        /// same configured database.
        /// </remarks>
        public bool ExecuteOneScript( string script, IActivityMonitor monitor = null )
        {
            using( var e = CreateExecutor( monitor ) )
            {
                return e.Execute( script );
            }
        }

        /// <summary>
        /// Simple execute scalar helper.
        /// The connection must be opened.
        /// </summary>
        /// <param name="select">Select clause.</param>
        /// <returns>The scalar (may be DBNull.Value) or null if no result has been returned.</returns>
        public object ExecuteScalar( string select )
        {
            CheckOpen();
            using( var cmd = new SqlCommand( select ) { Connection = _oCon } )
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Simple execute helper.
        /// The connection must be opened.
        /// </summary>
        /// <param name="cmd">The command text.</param>
        /// <param name="timeoutSecond">Timeout in seconds.</param>
        /// <returns>The number of rows.</returns>
        public int ExecuteNonQuery( string cmd, int timeoutSecond = -1 )
        {
            CheckOpen();
            using( var c = new SqlCommand( cmd ) { Connection = _oCon } )
            {
                if( timeoutSecond >= 0 ) c.CommandTimeout = timeoutSecond;
                return c.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes the command and returns the first row as an array of object values.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>An array of objects or null if nothing has been returned from database.</returns>
        public object[] ReadFirstRow( SqlCommand cmd )
        {
            CheckOpen();
            cmd.Connection = _oCon;
            using( SqlDataReader r = cmd.ExecuteReader( CommandBehavior.SingleRow ) )
            {
                if( !r.Read() ) return null;
                object[] res = new object[r.FieldCount];
                r.GetValues( res );
                return res;
            }
        }


        #region Private

        void OnConnStateChange( object sender, StateChangeEventArgs args )
        {
            Debug.Assert( _monitor != null );
            if( args.CurrentState == ConnectionState.Open )
                _monitor.Info( "Connected to database." );
            else _monitor.Info( "Disconnected from database." );
        }

        void OnConnInfo( object sender, SqlInfoMessageEventArgs args )
        {
            Debug.Assert( _monitor != null );
            foreach( SqlError err in args.Errors )
            {
                if( err.Class <= 10 )
                {
                    if( _missingDependencyIsError && err.Number == 2007 )
                    {
                        _monitor.Error( $"Missing Dependency (MissingDependencyIsError configuration is true for this object).\r\n"
                                      + "You can set MissingDependencyIsError to false for this object, or set IgnoreMissingDependencyIsError configuration to true to globally ignore this error (but it is better to correctly manage Requirements).\r\n"
                                      + "{err.Procedure} ({err.LineNumber}): {err.Message}" );
                    }
                    else _monitor.Info( $"{err.Procedure} ({err.LineNumber}): {err.Message}" );
                }
                else if( err.Class <= 16 )
                {
                    _monitor.Warn( $"{err.Procedure} ({err.LineNumber}): {err.Message}" );
                }
                else
                {
                    _monitor.Error( $"Sql Server error at '{err.Source}'\r\nClass='{err.Class}'\r\nMessage: '{err.Message}'\r\nProcedure: '{err.Procedure}'\r\nLineNumber: '{err.LineNumber}'\r\nNumber: '{err.Number}'\r\nState: '{err.State}'\r\nServer: '{err.Server}'" );
                }
            }
        }

        #endregion

        static string GetMasterConnectionString( string connectionString, out string currentDatabase )
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder( connectionString );
            currentDatabase = b.InitialCatalog;
            if( currentDatabase == "master" ) return connectionString;
            b.InitialCatalog = "master";
            return b.ToString();
        }
    }
}
