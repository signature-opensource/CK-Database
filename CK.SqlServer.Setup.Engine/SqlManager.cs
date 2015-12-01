#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Engine\SqlManager.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        static List<string> _protectedDatabaseNames = new List<string>() { "master", "msdb", "tempdb", "model" };

        readonly IActivityMonitor   _monitor;
        SqlConnectionProvider	    _oCon;
        bool				    	_checkTranCount;
        bool                        _ckCoreInstalled;
        bool                        _missingDependencyIsError;
        bool                        _ignoreMissingDependencyIsError;

        /// <summary>
        /// Initializes a new SqlManager.
        /// </summary>
        public SqlManager( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _monitor = monitor;
            _checkTranCount = true;
        }

        /// <summary>
        /// Gets the <see cref="SqlConnectionProvider"/> of this <see cref="SqlManager"/>.
        /// Null when the connection is closed.
        /// </summary>
        public SqlConnectionProvider Connection
        {
            get { return _oCon; }
        }

        void IDisposable.Dispose()
        {
            Close();
        }

        /// <summary>
        /// Close the connection. <see cref="Connection"/> becomes null.
        /// Can be called multiple times.
        /// </summary>
        public void Close()
        {
            if( _oCon != null )
            {
                _oCon.InternalConnection.StateChange -= new StateChangeEventHandler( OnConnStateChange );
                _oCon.InternalConnection.InfoMessage -= new SqlInfoMessageEventHandler( OnConnInfo );
                _oCon.Dispose();
                _oCon = null;
            }
        }

        void DoOpen( string connectionString, bool clearPoolFirst = false )
        {
            Debug.Assert( _oCon == null );
            try
            {
                _oCon = new SqlConnectionProvider( connectionString );
                if( clearPoolFirst ) SqlConnection.ClearPool( _oCon.InternalConnection );
                if( _monitor != null )
                {
                    _oCon.InternalConnection.StateChange += new StateChangeEventHandler( OnConnStateChange );
                    _oCon.InternalConnection.InfoMessage += new SqlInfoMessageEventHandler( OnConnInfo );
                }
                _oCon.ExplicitOpen();
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
        /// Opens a database from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <param name="autoCreate">False to not creating the database if it does not exist.</param>
        /// <returns>True on success.</returns>
        public bool OpenFromConnectionString( string connectionString, bool autoCreate = false )
        {
            using( _monitor.OpenInfo().Send( "Connection to {0}.", connectionString ) )
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
                        _monitor.Warn().Send( ex );
                        string name;
                        using( var master = new SqlConnectionProvider( GetMasterConnectionString( connectionString, out name ) ) )
                        {
                            try
                            {
                                _monitor.Info().Send( "Creating database '{0}'.", name );
                                master.ExecuteNonQuery( "create database " + name );
                            }
                            catch( Exception exCreate )
                            {
                                _monitor.Error().Send( exCreate );
                                return false;
                            }
                        }
                        try
                        {
                            DoOpen( connectionString, true );
                            return true;
                        }
                        catch( Exception exOpenCreated )
                        {
                            _monitor.Error().Send( exOpenCreated );
                            return false;
                        }
                    }
                    else
                    {
                        _monitor.Error().Send( ex );
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
        /// Gets or sets a <see cref="IActivityMonitor"/>. When a monitor is set,
        /// exceptions are redirected to it and this <see cref="SqlManager"/> does not throw 
        /// exceptions any more.
        /// </summary>
        public IActivityMonitor Monitor
        {
            get { return _monitor; }
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
            using( var cmd = new SqlCommand( "select OBJECT_DEFINITION(OBJECT_ID(@0))" ) )
            {
                cmd.Parameters.AddWithValue( "@0", schemaName );
                return (string)_oCon.ExecuteScalar( cmd );
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
            if( String.IsNullOrEmpty( schema )
                || schema.IndexOf( '\'' ) >= 0
                || schema.IndexOf( ';' ) >= 0 ) throw new ArgumentException( "schemaName" );
            try
            {
                using( var c = new SqlCommand( "CKCore.sSchemaDropAllObjects" ) )
                {
                    c.CommandType = CommandType.StoredProcedure;
                    c.Parameters.AddWithValue( "@SchemaName", schema );
                    _oCon.ExecuteNonQuery( c );
                    if( dropSchema )
                    {
                        c.CommandType = CommandType.Text;
                        c.CommandText = String.Format( "if exists(select 1 from sys.schemas where name = '{0}') drop schema {0};", schema );
                        _oCon.ExecuteNonQuery( c );
                    }
                }
                if( schema == "CKCore" ) _ckCoreInstalled = false;
                return true;
            }
            catch( Exception ex )
            {
                _monitor.Error().Send( ex );
                return false;
            }
        }

        void CheckAction( string action, string dbName )
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
            readonly IDisposable _connectionCloser;

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
                _databaseName = autoRestoreDatabase ? _command.Connection.Database : null;
                if( checkTransactionCount )
                {
                    _command.CommandText = "select @@TranCount;";
                    _tranCount = (int)_command.ExecuteScalar();
                }
                else _tranCount = -1;
                _connectionCloser = _manager.Connection.AcquireConnection( _command );
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
                    if( hasBeenTraced ) _monitor.Error().Send( e );
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
                _connectionCloser.Dispose();
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
                                    msg += " Attempting rollback: ";
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
                        if( _databaseName != null && _databaseName != _manager.Connection.InternalConnection.Database )
                        {
                            if( _monitor != null ) _monitor.Info().Send( "Current database automatically restored from {0} to {1}.", _manager.Connection.InternalConnection.Database, _databaseName );
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

        #region Private

        void OnConnStateChange( object sender, StateChangeEventArgs args )
        {
            Debug.Assert( _monitor != null );
            if( args.CurrentState == ConnectionState.Open )
                _monitor.Info().Send( "Connected to database." );
            else _monitor.Info().Send( "Disconnected from database." );
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

        public static string GetMasterConnectionString( string connectionString )
        {
            string current;
            return GetMasterConnectionString( connectionString, out current );
        }

        public static string GetMasterConnectionString( string connectionString, out string currentDatabase )
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder( connectionString );
            currentDatabase = b.InitialCatalog;
            if( currentDatabase == "master" ) return connectionString;
            b.InitialCatalog = "master";
            return b.ToString();
        }
    }
}
