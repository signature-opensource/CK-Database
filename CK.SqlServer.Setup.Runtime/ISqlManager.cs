using System;
using System.Collections.Generic;
using CK.Core;
namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Offers script execution facility and higher level database management (such as automatically 
    /// creating a database) for Sql server databases.
    /// </summary>
    public interface ISqlManager : IDisposable
    {
        /// <summary>
        /// Gets or sets a <see cref="IActivityMonitor"/>. When a monitor is set,
        /// exceptions are redirected to it and this <see cref="SqlManager"/> does not throw 
        /// exceptions any more.
        /// </summary>
        IActivityMonitor Monitor { get; set; }
        
        /// <summary>
        /// Gets the <see cref="SqlConnectionProvider"/> of this <see cref="SqlManager"/>.
        /// </summary>
        SqlConnectionProvider Connection { get; }

        /// <summary>
        /// True if the connection to the current database is managed directly by server and database name,
        /// false if the <see cref="OpenFromConnectionString"/> method has been used.
        /// </summary>
        bool IsAutoConnectMode { get; }

        /// <summary>
        /// Databases in this list will not be reseted nor created.
        /// </summary>
        IList<string> ProtectedDatabaseNames { get; }

        /// <summary>
        /// Gets or sets whether whenever a creation script is executed, the informational message
        /// 'The module 'X' depends on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
        /// must be logged as a <see cref="LogLevel.Error"/>. When false, this is a <see cref="LogLevel.Info"/>.
        /// Defaults to false.
        /// Note that if <see cref="IgnoreMissingDependencyIsError"/> is true, this property has no effect and a missing dependency will remain informational.
        /// </summary>
        bool MissingDependencyIsError { get; set; }

        /// <summary>
        /// Gets or sets whether <see cref="MissingDependencyIsError"/> must be ignored.
        /// When true, MissingDependencyIsError is always considered to be false.
        /// Defaults to true (a missing dependency is logged with <see cref="LogLevel.Info"/> level).
        /// </summary>
        bool IgnoreMissingDependencyIsError { get; set; }

        /// <summary>
        /// The currently active server.
        /// </summary>
        string Server { get; }

        /// <summary>
        /// The currently active database. Connection must be opened.
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// The currently active <i>server/database</i>.
        /// </summary>
        string ServerDatabaseName { get; }

        /// <summary>
        /// If we are in <see cref="IsAutoConnectMode"/>, the current connection string is:<br/>
        /// "Server=<see cref="Server"/>;Database=<see cref="DatabaseName"/>;Integrated Security=SSPI"<br/>
        /// else it is the original connection string given to <see cref="OpenFromConnectionString"/> method.
        /// </summary>
        string CurrentConnectionString { get; }

        /// <summary>
        /// True if the connection to the current database is opened. Can be called on a 
        /// disposed <see cref="SqlManager"/>.
        /// </summary>
        bool IsOpen();
        
        /// <summary>
        /// Opens a database from a connection string.
        /// If a <see cref="Monitor"/> is set, exceptions will be routed to it.
        /// </summary>
        /// <param name="connectionString">The connection string to the database.</param>
        /// <returns>
        /// If a <see cref="Monitor"/> is set, this method will return true or false 
        /// to indicate success.
        /// </returns>
        bool OpenFromConnectionString( string connectionString );

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
        bool Open( string server, string database );

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
        bool Open( string server, string database, bool autoCreate, out bool hasBeenCreated );

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
        bool OpenOrCreate( string server, string database );
        
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
        bool CreateDatabase( string databaseName );

        /// <summary>
        /// Ensures that the CKCore kernel is installed.
        /// </summary>
        /// <param name="monitor">The monitor to use. Can not be null.</param>
        /// <returns>True on success.</returns>
        bool EnsureCKCoreIsInstalled( IActivityMonitor monitor );


        /// <summary>
        /// The script is <see cref="IActivityMonitor.Trace"/>d (if <see cref="monitor"/> is not null).
        /// </summary>
        /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
        ISqlScriptExecutor CreateExecutor( IActivityMonitor monitor, bool checkTransactionCount = true, bool autoRestoreTargetDatabase = true );

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
        bool ExecuteOneScript( string script, IActivityMonitor monitor = null );

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
        bool ExecuteScripts( IEnumerable<string> scripts, IActivityMonitor monitor );

        /// <summary>
        /// Open a trusted connection on the root (master) database.
        /// </summary>
        /// <param name="server">Name of the instance server. If null or empty, '(local)' is assumed.</param>
        void Open( string server );

        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schemaName">Name of the schema. Must not be null nor empty.</param>
        /// <returns>
        /// Always true if no <see cref="Monitor"/> is set (an exception
        /// will be thrown in case of failure). If a <see cref="Monitor"/> is set,
        /// this method will return true or false to indicate success.
        /// </returns>
        bool SchemaDropAllObjects( string schemaName, bool dropSchema );
    }
}
