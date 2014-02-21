using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Offers script execution facility and higher level database management (such as automatically 
    /// creating a database) for Sql server databases.
    /// </summary>
    public class SqlManager : ISqlManager
    {
        SqlConnectionProvider	_oCon;
        List<string>			_protectedDatabaseNames;
        string 					_fromConnectionString;
        IActivityMonitor         _monitor;
        bool					_checkTranCount;
        bool                    _ckCoreInstalled;
        bool                    _missingDependencyIsError;
        bool                    _ignoreMissingDependencyIsError;

        /// <summary>
        /// Default and only constructor.
        /// </summary>
        public SqlManager()
        {
            _oCon = new SqlConnectionProvider();
            _protectedDatabaseNames = new List<string>() { "master", "msdb", "tempdb", "model" };
            _checkTranCount = true;
        }

        /// <summary>
        /// Creates a new <see cref="SqlManager"/> bound to a server and a database with an attempt to create if it does not exist.
        /// </summary>
        /// <param name="server">Server name.</param>
        /// <param name="database">Database name.</param>
        /// <param name="monitor">
        /// Monitor to use, when null an exception is thrown on error. 
        /// Otherwise any exceptions are routed to it and it is associated as the <see cref="SqlManager.Monitor"/>.</param>
        /// <returns>A new <see cref="SqlManager"/> or null if an error occurred and no <paramref name="monitor"/> is provided.</returns>
        public static SqlManager OpenOrCreate( string server, string database, IActivityMonitor monitor = null )
        {
            SqlManager m = new SqlManager();
            if( monitor != null ) m.Monitor = monitor;
            return m.OpenOrCreate( server, database ) ? m : null;
        }

        /// <summary>
        /// Gets the <see cref="SqlConnectionProvider"/> of this <see cref="SqlManager"/>.
        /// </summary>
        public SqlConnectionProvider Connection
        {
            get { return _oCon; }
        }

        /// <summary>
        /// True if the connection to the current database is managed directly by server and database name,
        /// false if the <see cref="OpenFromConnectionString"/> method has been used.
        /// </summary>
        public bool IsAutoConnectMode
        {
            get { return _fromConnectionString != null; }
        }

        /// <summary>
        /// Databases in this list will not be reseted nor created.
        /// </summary>
        public IList<string> ProtectedDatabaseNames
        {
            get { return _protectedDatabaseNames; }
        }

        /// <summary>
        /// Closes the connection if needed.
        /// </summary>
        public void Dispose()
        {
            Monitor = null;
            if( _oCon != null )
            {
                _oCon.Dispose();
                _oCon = null;
            }
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
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
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
        /// If we are in <see cref="IsAutoConnectMode"/>, the current connection string is:<br/>
        /// "Server=<see cref="Server"/>;Database=<see cref="DatabaseName"/>;Integrated Security=SSPI"<br/>
        /// else it is the original connection string given to <see cref="OpenFromConnectionString"/> method.
        /// </summary>
        public string CurrentConnectionString
        {
            get { return _fromConnectionString ?? String.Format( "Server={0};Database={1};Integrated Security=SSPI;", Server, DatabaseName ); }
        }

        /// <summary>
        /// Opens a database from a connection string.
        /// If a <see cref="Monitor"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <returns>
        /// If a <see cref="Monitor"/> is set, this method will return true or false 
        /// to indicate success.
        /// </returns>
        public bool OpenFromConnectionString( string connectionString )
        {
            if( _monitor != null ) _monitor.OpenInfo().Send(  "Connection" );
            try
            {
                _oCon.ConnectionString = connectionString;
                _oCon.Open();
                _fromConnectionString = connectionString;
                return true;
            }
            catch( Exception ex )
            {
                _oCon.Close();
                if( _monitor != null )
                {
                    _monitor.Error().Send( ex );
                    return false;
                }
                throw;
            }
            finally
            {
                if( _monitor != null ) _monitor.CloseGroup( null );
            }
        }

        /// <summary>
        /// The currently active server.
        /// </summary>
        public string Server
        {
            get { return _oCon.InternalConnection.DataSource; }
        }

        /// <summary>
        /// The currently active database. Connection must be opened.
        /// </summary>
        public string DatabaseName
        {
            get { return _oCon.InternalConnection.Database; }
        }

        /// <summary>
        /// The currently active <i>server/database</i>.
        /// </summary>
        public string ServerDatabaseName
        {
            get { return Server + '/' + DatabaseName; }
        }

        /// <summary>
        /// Gets or sets a <see cref="IActivityMonitor"/>. When a monitor is set,
        /// exceptions are redirected to it and this <see cref="SqlManager"/> does not throw 
        /// exceptions any more.
        /// </summary>
        public IActivityMonitor Monitor
        {
            get { return _monitor; }
            set
            {
                if( _monitor != value )
                {
                    if( _monitor == null && value != null )
                    {
                        _oCon.InternalConnection.StateChange += new StateChangeEventHandler( OnConnStateChange );
                        _oCon.InternalConnection.InfoMessage += new SqlInfoMessageEventHandler( OnConnInfo );
                    }
                    else if( _monitor != null && value == null )
                    {
                        _oCon.InternalConnection.StateChange -= new StateChangeEventHandler( OnConnStateChange );
                        _oCon.InternalConnection.InfoMessage -= new SqlInfoMessageEventHandler( OnConnInfo );
                    }
                    _monitor = value;
                }
            }
        }

        /// <summary>
        /// True if the connection to the current database is opened. Can be called on a 
        /// disposed <see cref="SqlManager"/>.
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            return _oCon != null && _oCon.InternalConnection.State == System.Data.ConnectionState.Open;
        }

        /// <summary>
        /// Opens a database (do not try to create it if it does not exist).
        /// If a <see cref="IActivityMonitor"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="server">Server name. May be null or empty, in this case '(local)' is assumed.</param>
        /// <param name="database">The database name to open.</param>
        /// <returns>
        /// Always true if no <see cref="Monitor"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.
        /// </returns>
        public bool Open( string server, string database )
        {
            bool hasBeenCreated;
            return Open( server, database, false, out hasBeenCreated );
        }

        /// <summary>
        /// Opens an existing database or creates it if it does not exist.
        /// If a <see cref="IActivityMonitor"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="server">Server name. May be null or empty, in this case '(local)' is assumed.</param>
        /// <param name="database">The database name to open or create.</param>
        /// <returns>
        /// Always true if no <see cref="Monitor"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.
        /// </returns>
        public bool OpenOrCreate( string server, string database )
        {
            bool hasBeenCreated;
            return Open( server, database, true, out hasBeenCreated );
        }

        /// <summary>
        /// Try to connect and open a database. Can create it if it does not exist.
        /// </summary>
        /// <param name="server">Server name. May be null or empty, in this case '(local)' is assumed.</param>
        /// <param name="database">The database name to open (or create).</param>
        /// <param name="autoCreate">True to create the database if it does not exist.</param>
        /// <param name="hasBeenCreated">An output parameter that is set to true if the database has been created.</param>
        /// <returns>
        /// Always true if no <see cref="Monitor"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.
        /// </returns>
        /// <remarks>
        /// This method automatically closes the <see cref="Connection"/> if needed.
        /// </remarks>
        public bool Open( string server, string database, bool autoCreate, out bool hasBeenCreated )
        {
            if( _fromConnectionString != null )
            {
                _fromConnectionString = null;
                _oCon.Close();
            }
            hasBeenCreated = false;
            if( server == null )
                server = String.Empty;
            else server = server.Trim();
            if( server.Length == 0 ) server = "(local)";

            if( _monitor != null ) _monitor.OpenInfo().Send( "Connection to {0}/{1}", server, database );
            try
            {
                if( _oCon.InternalConnection.State == System.Data.ConnectionState.Closed || Server != server )
                {
                    _oCon.ConnectionString = "Integrated Security=SSPI;Database=master;Server=" + server;
                    _oCon.Open();
                }
                if( database.Length > 0 )
                {
                    bool success = false;
                    try
                    {
                        _oCon.InternalConnection.ChangeDatabase( database );
                        success = true;
                    }
                    catch
                    {
                        if( !autoCreate ) throw;
                        success = CreateDatabase( database );
                        if( success ) hasBeenCreated = true;
                    }
                    _oCon.ConnectionString = CurrentConnectionString;
                    _oCon.Open();
                    return success;
                }
                return true;
            }
            catch( Exception e )
            {
                if( _monitor == null ) throw new Exception( String.Format( "Unable to open {0}/{1}", server, database ), e );
                _monitor.Error().Send( e );
                return false;
            }
            finally
            {
                if( _monitor != null ) _monitor.CloseGroup( null );
            }
        }

        /// <summary>
        /// Open a trusted connection on the root (master) database.
        /// </summary>
        /// <param name="server">Name of the instance server. If null or empty, '(local)' is assumed.</param>
        public void Open( string server )
        {
            Open( server, "master" );
        }

        /// <summary>
        /// Try to create a database. The connection must be opened (but it can be on another database).
        /// On success, the connection is bound to the newly created database in <see cref="IsAutoConnectMode"/> (existing 
        /// connection string set by a previous call to <see cref="OpenFromConnectionString"/> is lost).
        /// </summary>
        /// <param name="databaseName">
        /// The name of the database to create. 
        /// Must not belong to <see cref="ProtectedDatabaseNames"/> list.
        /// </param>
        /// <returns>Always true if no <see cref="Monitor"/> is set (an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.</returns>
        public bool CreateDatabase( string databaseName )
        {
            SqlCommand cmd = new SqlCommand( "create database " + databaseName );
            try
            {
                CheckAction( "create", databaseName );
                _oCon.InternalConnection.ChangeDatabase( "master" );
                _oCon.ExecuteNonQuery( cmd );
                _oCon.InternalConnection.ChangeDatabase( databaseName ); 
                // Refresh all cached connections.
                SqlConnection.ClearAllPools();
                _oCon.ConnectionString = CurrentConnectionString;
                _oCon.Open();
                return true;
            }
            catch( Exception e )
            {
                if( _monitor != null )
                {
                    _monitor.Error().Send( e );
                    return false;
                }
                throw;
            }
            finally
            {
                cmd.Dispose();
            }
        }

        /// <summary>
        /// Ensures that the CKCore kernel is installed.
        /// </summary>
        /// <param name="monitor">The monitor to use. Can not be null.</param>
        /// <returns>True on success.</returns>
        public bool EnsureCKCoreIsInstalled( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            if( !_ckCoreInstalled )
            {
                _ckCoreInstalled = SqlCKCoreInstaller.Install( this, monitor );
            }
            return _ckCoreInstalled;
        }

        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schemaName">Name of the schema. Must not be null nor empty.</param>
        /// <returns>Always true if no <see cref="Monitor"/> is set (an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.</returns>
        public bool SchemaDropAllObjects( string schemaName, bool dropSchema )
        {
            if( String.IsNullOrEmpty( schemaName ) 
                || schemaName.IndexOf( '\'' ) >= 0
                || schemaName.IndexOf( ';' ) >= 0 ) throw new ArgumentException( "schemaName" );
            try
            {
                using( var c = new SqlCommand( "CKCore.sSchemaDropAllObjects" ) )
                {
                    c.CommandType = CommandType.StoredProcedure;
                    c.Parameters.AddWithValue( "@SchemaName", schemaName );
                    _oCon.ExecuteNonQuery( c );
                    if( dropSchema )
                    {
                        c.CommandType = CommandType.Text;
                        c.CommandText = String.Format( "if exists(select 1 from sys.schemas where name = '{0}') drop schema {0};", schemaName );
                        _oCon.ExecuteNonQuery( c );
                    }
                }
                if( schemaName == "CKCore" ) _ckCoreInstalled = false;
            }
            catch( Exception ex )
            {
                if( _monitor != null )
                {
                    _monitor.Error().Send( ex );
                    return false;
                }
                throw;
            }
            return true;
        }

        private void CheckAction( string action, string dbName )
        {
            if( dbName == null || dbName.Length == 0 || _protectedDatabaseNames.Contains( dbName ) )
            {
                throw new Exception( String.Format( "Attempt to {0} database '{1}'.", action, dbName ) );
            }
        }

        class SqlExecutor : ISqlScriptExecutor
        {
            readonly SqlManager _manager;
            readonly SqlCommand _command;
            readonly IActivityMonitor _monitor;
            readonly int _tranCount;
            readonly string _databaseName;
            readonly bool _mustClose;

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
                // 8 minutes timeout... should be enough!
                _command.CommandTimeout = 8 * 60;
                _command.Connection = _manager.Connection.InternalConnection;
                _databaseName = autoRestoreDatabase ? _manager.DatabaseName : null;
                if( checkTransactionCount )
                {
                    _command.CommandText = "select @@TranCount;";
                    _tranCount = (int)_command.ExecuteScalar();
                }
                else _tranCount = -1;
                _manager.Connection.AcquireConnection( _command, out _mustClose );
            }

            public bool Execute( string script )
            {
                if( script == null ) throw new ArgumentNullException( "script" );
                LastSucceed = false;
                try
                {
                    script = script.Trim();
                    if( script.Length > 0 )
                    {
                        _command.CommandText = script;
                        if( _monitor != null )
                        {
                            _monitor.Trace().Send( script );
                            _monitor.Trace().Send( "GO" );
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
                    if( _monitor.ActualFilter.Line == LogLevelFilter.Trace ) _monitor.Error().Send( e );
                    else 
                    {
                        // If the text is not already logged, then we unconditionally log it below the error.
                        using( _monitor.OpenError().Send( e ) )
                        {
                            _monitor.Info().Send( script );
                        }
                    }
                }
                return LastSucceed;
            }

            public void Dispose()
            {
                _manager.Connection.ReleaseConnection( _command, _mustClose );
                _command.Dispose();
                if( _manager.IsOpen() )
                {
                    try
                    {
                        if( _tranCount >= 0 )
                        {
                            int tranCountAfter = (int)_manager.Connection.ExecuteScalar( "select @@TranCount" );
                            if( _tranCount != tranCountAfter )
                            {
                                string msg = String.Format( "Transaction count differ: {0} before, {1} after.", _tranCount, tranCountAfter );
                                int nbRollbak = tranCountAfter - _tranCount;
                                if( _tranCount == 0 && nbRollbak > 0 )
                                {
                                    msg += " Attempting rollbak: ";
                                    try
                                    {
                                        _manager.Connection.ExecuteNonQuery( "rollback" );
                                        msg += "Succeed.";
                                    }
                                    catch( Exception ex )
                                    {
                                        msg += "Failed -> " + ex.Message;
                                    }
                                }
                                if( _monitor != null ) _monitor.Error().Send( msg );
                                else if( LastSucceed ) throw new Exception( msg );
                            }
                        }                       
                        if( _databaseName != null && _databaseName != _manager.DatabaseName )
                        {
                            if( _monitor != null ) _monitor.Info().Send( "Current database automatically restored from {0} to {1}.", _manager.DatabaseName, _databaseName );
                            _command.Connection.ChangeDatabase( _databaseName );
                        }
                    }
                    catch( Exception ex )
                    {
                        if( _monitor != null ) _monitor.OpenWarn().Send( ex );
                        else
                        {
                            if( LastSucceed ) throw;
                            // When an error already occurred, we do not rethrow the internal exception.
                        }
                    }
                }

            }

        }

        /// <summary>
        /// The script is <see cref="IActivityMonitor.Trace"/>d (if <see cref="monitor"/> is not null).
        /// </summary>
        /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
        public ISqlScriptExecutor CreateExecutor( IActivityMonitor monitor, bool checkTransactionCount = true, bool autoRestoreDatabase = true )
        {
            return new SqlExecutor( this, monitor, checkTransactionCount, autoRestoreDatabase );
        }

        public bool ExecuteScripts( IEnumerable<string> scripts, IActivityMonitor monitor )
        {
            using( var e = CreateExecutor( monitor ) )
            {
                return e.Execute( scripts ) == 0;
            }
        }

        /// <summary>
        /// Executes one script (no GO separator must exist inside). 
        /// The script is <see cref="IActivityMonitor.Trace"/>d (if <see cref="monitor"/> is not null).
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

        #region Private

        private void OnConnStateChange( object sender, StateChangeEventArgs args )
        {
            Debug.Assert( _monitor != null );
            if( args.CurrentState == ConnectionState.Open )
                _monitor.Info().Send( "Connected to {0}.", ServerDatabaseName );
            else _monitor.Info().Send( "Disconnected from {0}.", ServerDatabaseName );
        }

        private void OnConnInfo( object sender, SqlInfoMessageEventArgs args )
        {
            Debug.Assert( _monitor != null );
            foreach( SqlError err in args.Errors )
            {
                if( err.Class <= 10 )
                {
                    if( _missingDependencyIsError && err.Number == 2007 )
                    {
                        _monitor.Error().Send( "Missing Dependency (MissingDependencyIsError configuration is true for this object).\r\n"
                                      + "You can set MissingDependencyIsError to false for this object, or set IgnoreMissingDependencyIsError configuration to true to globally ignore this error (but it is better to correctly manage Requirements).\r\n"
                                      + "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                    }
                    else _monitor.Info().Send( "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                }
                else if( err.Class <= 16 )
                {
                    _monitor.Warn().Send( "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                }
                else
                {
                    _monitor.Error().Send( "Sql Server error at '{0}'\r\nClass='{1}'\r\nMessage: '{2}'\r\nProcedure: '{6}'\r\nLineNumber: '{7}'\r\nNumber: '{3}'\r\nState: '{4}'\r\nServer: '{5}'", 
                                        err.Source, 
                                        err.Class, 
                                        err.Message, 
                                        err.Number, 
                                        err.State, 
                                        err.Server, 
                                        err.Procedure, 
                                        err.LineNumber );
                }
            }
        }

        #endregion
    }
}
