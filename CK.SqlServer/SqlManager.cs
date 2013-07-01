using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Offers script execution facility and higher level database management (such as automatically 
    /// creating a database) for Sql server databases.
    /// </summary>
    public class SqlManager : IDisposable
    {
        SqlConnectionProvider	_oCon;
        List<string>			_protectedDatabaseNames;
        string 					_fromConnectionString;
        DatabaseInfo			_curDbInfo;
        IActivityLogger         _logger;
        bool					_checkTranCount;
        bool                    _ckCoreInstalled;
        bool                    _missingDependencyIsError;
        bool                    _ignoreMissingDependencyIsError;

        /// <summary>
        /// Main database properties.
        /// </summary>
        public class DatabaseInfo
        {
            public readonly Exception	Error;
            public readonly string		ErrorMessage;
            public readonly string      ComputerNamePhysicalNetBIOS;
            /// <summary>
            /// True if the ComputerNamePhysicalNetBIOS is the same as the <see cref="Environment.MachineName"/>.
            /// False if the database server is running on another machine.
            /// </summary>
            public readonly bool        IsLocalDatabase;
            public readonly bool        IsFullTextEnabled;
            public readonly string		Server;
            public readonly string		ServerVersion;
            public readonly string		Name;
            public readonly bool		Exists;
            public readonly string		Size;
            public readonly string		Owner;
            public readonly int			SqlObjectCount;
            public readonly string		CreatedDate;

            public readonly IReadOnlyList<FileInfo>	Files;

            internal DatabaseInfo( SqlManager m, string databaseName )
            {
                Name = databaseName;
                SqlCommand cmd = null;
                IDataReader result = null;
                try
                {
                    Server = m.Server;

                    cmd = new SqlCommand( String.Format( "select databasepropertyex('{0}', 'Status')", databaseName ) );
                    string status = (string)m.Connection.ExecuteScalar( cmd );
                    if( status == null )
                    {
                        Exists = false;
                        throw new ApplicationException( "Unknown Database." );
                    }
                    Exists = true;
                    if( status != "ONLINE" )
                        throw new ApplicationException( String.Format( "Database not available. Status = {0}.", status ) );

                    Server = m.Server;
                    ServerVersion = m.Connection.InternalConnection.ServerVersion;

                    cmd.CommandText = String.Format( "select count(*) from {0}.dbo.sysobjects", databaseName );
                    SqlObjectCount = (int)m.Connection.ExecuteScalar( cmd );

                    cmd.CommandText = String.Format( "select databaseproperty('{0}', 'IsFulltextEnabled')", databaseName );
                    object answer = m.Connection.ExecuteScalar( cmd );
                    IsFullTextEnabled = answer != null && (int)answer == 1;

                    cmd.CommandText = "select serverproperty('ComputerNamePhysicalNetBIOS')";
                    answer = m.Connection.ExecuteScalar( cmd );
                    ComputerNamePhysicalNetBIOS = (string)answer;
                    IsLocalDatabase = ComputerNamePhysicalNetBIOS == Environment.MachineName;

                    cmd.CommandText = String.Format( "exec sp_helpdb '{0}'", databaseName );
                    result = m.Connection.AcquireReader( cmd );
                    result.Read();

                    Size = result.GetString( 1 ).Trim();
                    Owner = result.IsDBNull( 2 ) ? "---" : result.GetString( 2 );
                    CreatedDate = result.GetString( 4 );
                    result.NextResult();
                    List<FileInfo> files = new List<FileInfo>();
                    int iFileName = result.GetOrdinal( "filename" );
                    while( result.Read() )
                    {
                        files.Add( new FileInfo( result.GetString( iFileName ).ToString().Trim() ) );
                    }
                    Files = files.ToReadOnlyList();
                }
                catch( Exception e )
                {
                    Error = e;
                    ErrorMessage = e.Message;
                }
                finally
                {
                    if( result != null ) m.Connection.ReleaseReader( result, cmd );
                    cmd.Dispose();
                }
            }

            public void Write( TextWriter w, bool withFiles )
            {
                if( ErrorMessage != null )
                {
                    w.WriteLine( "Database = {0}", Name );
                    w.WriteLine( "Error = {0}", ErrorMessage );
                }
                else
                {
                    w.WriteLine( "Database = {0} ({1}, {2} objects)", Name, Size, SqlObjectCount );
                    w.WriteLine( "Owned by {0}, created on {1}", Owner, CreatedDate );
                    w.WriteLine( "Server = {0}, Version = {1}", Server, ServerVersion );
                    w.WriteLine( "ComputerNamePhysicalNetBIOS = {0}", ComputerNamePhysicalNetBIOS );
                    if( withFiles )
                    {
                        int i = 0;
                        foreach( FileInfo f in Files )
                        {
                            w.WriteLine( "File {0}:{1}", ++i, f.FullName );
                        }
                    }
                }
            }

        }

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
        /// <param name="logger">
        /// Logger to use, when null an exception is thrown on error. 
        /// Otherwise any exceptions are routed to it and it is associated as the <see cref="SqlManager.Logger"/>.</param>
        /// <returns>A new <see cref="SqlManager"/> or null if an error occured and no <paramref name="logger"/> is provided.</returns>
        public static SqlManager OpenOrCreate( string server, string database, IActivityLogger logger = null )
        {
            SqlManager m = new SqlManager();
            if( logger != null ) m.Logger = logger;
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
        /// <returns></returns>
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
            Logger = null;
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
        /// If a <see cref="Logger"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <returns>
        /// If a <see cref="Logger"/> is set, this method will return true or false 
        /// to indicate success.
        /// </returns>
        public bool OpenFromConnectionString( string connectionString )
        {
            if( _logger != null ) _logger.OpenGroup( LogLevel.Info,  "Connection" );
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
                if( _logger != null )
                {
                    _logger.Error( ex );
                    return false;
                }
                throw;
            }
            finally
            {
                if( _logger != null ) _logger.CloseGroup( null );
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
        /// Gets or sets a <see cref="IActivityLogger"/>. When a logger is set,
        /// exceptions are redirected to it and this <see cref="SqlManager"/> does not throw 
        /// exceptions any more.
        /// </summary>
        public IActivityLogger Logger
        {
            get { return _logger; }
            set
            {
                if( _logger != value )
                {
                    if( _logger == null && value != null )
                    {
                        _oCon.InternalConnection.StateChange += new StateChangeEventHandler( OnConnStateChange );
                        _oCon.InternalConnection.InfoMessage += new SqlInfoMessageEventHandler( OnConnInfo );
                    }
                    else if( _logger != null && value == null )
                    {
                        _oCon.InternalConnection.StateChange -= new StateChangeEventHandler( OnConnStateChange );
                        _oCon.InternalConnection.InfoMessage -= new SqlInfoMessageEventHandler( OnConnInfo );
                    }
                    _logger = value;
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
        /// If a <see cref="IActivityLogger"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="server">Server name. May be null or empty, in this case '(local)' is assumed.</param>
        /// <param name="database">The database name to open.</param>
        /// <returns>
        /// Always true if no <see cref="Logger"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Logger"/> is set,
        /// this method will return true or false to indicate success.
        /// </returns>
        public bool Open( string server, string database )
        {
            bool hasBeenCreated;
            return Open( server, database, false, out hasBeenCreated );
        }

        /// <summary>
        /// Opens an existing database or creates it if it does not exist.
        /// If a <see cref="IActivityLogger"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="server">Server name. May be null or empty, in this case '(local)' is assumed.</param>
        /// <param name="database">The database name to open or create.</param>
        /// <returns>
        /// Always true if no <see cref="Logger"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Logger"/> is set,
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
        /// Always true if no <see cref="Logger"/> is set (otherwise an exception
        /// will be thrown in case of failure). If a <see cref="Logger"/> is set,
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

            if( _logger != null ) _logger.OpenGroup( LogLevel.Info, "Connection to {0}/{1}", server, database );
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
                if( _logger == null ) throw new Exception( String.Format( "Unable to open {0}/{1}", server, database ), e );
                _logger.Error( e );
                return false;
            }
            finally
            {
                if( _logger != null ) _logger.CloseGroup( null );
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
        /// On success, the connection si bound to the newly created database in <see cref="IsAutoConnectMode"/> (existing 
        /// connection string set by a previous call to <see cref="OpenFromConnectionString"/> is lost).
        /// </summary>
        /// <param name="databaseName">
        /// The name of the database to create. 
        /// Must not belong to <see cref="ProtectedDatabaseNames"/> list.
        /// </param>
        /// <returns>Always true if no <see cref="Logger"/> is set (an exception
        /// will be thrown in case of failure). If a <see cref="Logger"/> is set,
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
                if( _logger != null )
                {
                    _logger.Error( e );
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
        /// <param name="logger">The logger to use. Can not be null.</param>
        /// <returns>True on success.</returns>
        public bool EnsureCKCoreIsInstalled( IActivityLogger logger )
        {
            if( logger == null ) throw new ArgumentNullException( "logger" );
            if( !_ckCoreInstalled )
            {
                _ckCoreInstalled = SqlCKCoreInstaller.Install( this, logger );
            }
            return _ckCoreInstalled;
        }

        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schemaName">Name of the schema. Must not be null nor empty.</param>
        /// <returns>Always true if no <see cref="Logger"/> is set (an exception
        /// will be thrown in case of failure). If a <see cref="Logger"/> is set,
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
                if( _logger != null )
                {
                    _logger.Error( ex );
                    return false;
                }
                throw;
            }
            return true;
        }

        /// <summary>
        /// Gets the <see cref="DatabaseInfo"/> for a database.
        /// </summary>
        /// <param name="databaseName">
        /// Name of the database. Use null to read information for the current database.
        /// </param>
        public DatabaseInfo GetDBInfo( string databaseName )
        {
            if( databaseName == null ) return GetDBInfo( false );
            return new DatabaseInfo( this, databaseName );
        }

        /// <summary>
        /// Gets the <see cref="DatabaseInfo"/> for the current database.
        /// </summary>
        /// <param name="refresh">True to refresh the information from the database.</param>
        public DatabaseInfo GetDBInfo( bool refresh )
        {
            if( _curDbInfo == null
                || refresh
                || _curDbInfo.Name != DatabaseName )
            {
                _curDbInfo = new DatabaseInfo( this, DatabaseName );
            }
            return _curDbInfo;
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
            readonly IActivityLogger _logger;
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

            internal SqlExecutor( SqlManager m, IActivityLogger logger, bool checkTransactionCount, bool autoRestoreDatabase )
            {
                _manager = m;
                _logger = logger;
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
                        if( _logger != null )
                        {
                            _logger.Trace( script );
                            _logger.Trace( "GO" );
                        }
                        _command.ExecuteNonQuery();
                    }
                    LastSucceed = true;
                }
                catch( Exception e )
                {
                    FailCount = FailCount + 1;
                    if( _logger == null ) throw;
                    // If the logger is tracing, the text has already been logged.
                    if( _logger.Filter <= LogLevelFilter.Trace ) _logger.Error( e );
                    else 
                    {
                        // If the text is not already logged, then we unconditionnaly log it below the error.
                        using( _logger.OpenGroup( LogLevel.Error, e ) )
                        {
                            _logger.UnfilteredLog( ActivityLogger.RegisteredTags.EmptyTrait, LogLevel.Info, script, DateTime.UtcNow );
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
                                if( _logger != null ) _logger.Error( msg );
                                else if( LastSucceed ) throw new Exception( msg );
                            }
                        }                       
                        if( _databaseName != null && _databaseName != _manager.DatabaseName )
                        {
                            if( _logger != null ) _logger.Info( "Current database automatically restored from {0} to {1}.", _manager.DatabaseName, _databaseName );
                            _command.Connection.ChangeDatabase( _databaseName );
                        }
                    }
                    catch( Exception ex )
                    {
                        if( _logger != null ) _logger.OpenGroup( LogLevel.Warn, ex );
                        else
                        {
                            if( LastSucceed ) throw;
                            // When an error already occured, we do not rethrow the internal exception.
                        }
                    }
                }

            }

        }

        /// <summary>
        /// The script is <see cref="IActivityLogger.Trace"/>d (if <see cref="logger"/> is not null).
        /// </summary>
        /// <param name="logger">The logger to use. Null to not log anything (and throw exception on error).</param>
        public ISqlScriptExecutor CreateExecutor( IActivityLogger logger, bool checkTransactionCount = true, bool autoRestoreDatabase = true )
        {
            return new SqlExecutor( this, logger, checkTransactionCount, autoRestoreDatabase );
        }

        public bool ExecuteScripts( IEnumerable<string> scripts, IActivityLogger logger )
        {
            using( var e = CreateExecutor( logger ) )
            {
                return e.Execute( scripts ) == 0;
            }
        }

        /// <summary>
        /// Executes one script (no GO separator must exist inside). 
        /// The script is <see cref="IActivityLogger.Trace"/>d (if <see cref="logger"/> is not null).
        /// </summary>
        /// <param name="logger">The logger to use. Null to not log anything (and throw exception on error).</param>
        /// <param name="script">The script to execute.</param>
        /// <returns>
        /// Always true if <paramref name="logger"/> is null since otherwise an exception
        /// will be thrown in case of failure. 
        /// If a logger is set, this method will return true or false to indicate success.
        /// </returns>
        /// <remarks>
        /// At the end of the execution, the current database is checked and if it has changed,
        /// the connection is automatically restored onto the original database.
        /// This behavior enables the use of <code>Use OtherDbName</code> commands from inside 
        /// any script and guaranty that, at the beginning of a script, we always are on the 
        /// same configured database.
        /// </remarks>
        public bool ExecuteOneScript( string script, IActivityLogger logger = null )
        {
            using( var e = CreateExecutor( logger ) )
            {
                return e.Execute( script );
            }
        }

        #region Private

        private void OnConnStateChange( object sender, StateChangeEventArgs args )
        {
            Debug.Assert( _logger != null );
            if( args.CurrentState == ConnectionState.Open )
                _logger.Info( "Connected to {0}.", ServerDatabaseName );
            else _logger.Info( "Disconnected from {0}.", ServerDatabaseName );
        }

        private void OnConnInfo( object sender, SqlInfoMessageEventArgs args )
        {
            Debug.Assert( _logger != null );
            foreach( SqlError err in args.Errors )
            {
                if( err.Class <= 10 )
                {
                    if( _missingDependencyIsError && err.Number == 2007 )
                    {
                        _logger.Error( "Missing Dependency (MissingDependencyIsError configuration is true for this object).\r\n"
                                      + "You can set MissingDependencyIsError to false for this object, or set IgnoreMissingDependencyIsError configuration to true to globally ignore this error (but it is better to correctly manage Requirements).\r\n"
                                      + "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                    }
                    else _logger.Info( "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                }
                else if( err.Class <= 16 )
                {
                    _logger.Warn( "{0} ({1}): {2}", err.Procedure, err.LineNumber, err.Message );
                }
                else
                {
                    _logger.Error( "Sql Server error at '{0}'\r\nClass='{1}'\r\nMessage: '{2}'\r\nProcedure: '{6}'\r\nLineNumber: '{7}'\r\nNumber: '{3}'\r\nState: '{4}'\r\nServer: '{5}'", 
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
