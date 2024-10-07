using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer.Setup;


/// <summary>
/// Offers script execution facility and higher level database management (such as automatically 
/// creating a database) for Sql server databases.
/// This abstraction hides the actual database and enables fake implementations.
/// </summary>
public interface ISqlManagerBase : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IActivityMonitor"/>. 
    /// Must never be null.
    /// </summary>
    IActivityMonitor Monitor { get; }

    /// <summary>
    /// Gets or sets whether whenever a creation script is executed, the informational message
    /// 'The module 'X' depends on the missing object 'Y'. The module will still be created; however, it cannot run successfully until the object exists.' 
    /// must be logged as a <see cref="Core.LogLevel.Error"/>. When false, this is a <see cref="Core.LogLevel.Info"/>.
    /// Defaults to false.
    /// Note that if <see cref="IgnoreMissingDependencyIsError"/> is true, this property has no effect and a missing dependency will remain informational.
    /// </summary>
    bool MissingDependencyIsError { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="MissingDependencyIsError"/> must be ignored.
    /// When true, MissingDependencyIsError is always considered to be false.
    /// Defaults to true (a missing dependency is logged with <see cref="Core.LogLevel.Info"/> level).
    /// </summary>
    bool IgnoreMissingDependencyIsError { get; set; }

    /// <summary>
    /// Opens a database from a connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <param name="autoCreate">True to automatically create the database if it does not exist.</param>
    /// <returns>True on success, false otherwise.</returns>
    bool OpenFromConnectionString( string connectionString, bool autoCreate = false );

    /// <summary>
    /// Ensures that the CKCore kernel is installed.
    /// </summary>
    /// <param name="monitor">The monitor to use. Can not be null.</param>
    /// <returns>True on success.</returns>
    bool EnsureCKCoreIsInstalled( IActivityMonitor monitor );

    /// <summary>
    /// The script is traced (if <paramref name="monitor"/> is not null).
    /// </summary>
    /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
    /// <param name="checkTransactionCount">By default, transaction count is checked: it must be the same before and after the execution.</param>
    /// <param name="autoRestoreDatabase">By default, if the script USE another database, the initial one is automatically restored.</param>
    ISqlScriptExecutor CreateExecutor( IActivityMonitor monitor, bool checkTransactionCount = true, bool autoRestoreDatabase = true );

    /// <summary>
    /// Executes one script (no GO separator must exist inside). 
    /// The script is traced (if <paramref name="monitor"/> is not null).
    /// </summary>
    /// <param name="monitor">The monitor to use. Null to not log anything (and throw exception on error).</param>
    /// <param name="script">The script to execute.</param>
    /// <returns>
    /// Always true if <paramref name="monitor"/> is null since otherwise an exception
    /// will be thrown in case of failure. 
    /// If a monitor is provided, this method will return true or false to indicate success.
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
    /// If a monitor is provided, this method will return true or false to indicate success.
    /// </returns>
    bool ExecuteScripts( IEnumerable<string> scripts, IActivityMonitor monitor );

}
