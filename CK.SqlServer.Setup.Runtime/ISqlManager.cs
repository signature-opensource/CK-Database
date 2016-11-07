using System;
using System.Collections.Generic;
using CK.Core;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Extends <see cref="ISqlManagerBase"/> to offer minimal helpers
    /// to query the database.
    /// </summary>
    public interface ISqlManager : ISqlManagerBase
    {
        /// <summary>
        /// Gets the <see cref="SqlConnectionProvider"/> of this <see cref="ISqlManager"/>.
        /// </summary>
        SqlConnectionProvider Connection { get; }

        /// <summary>
        /// True if the connection to the current database is opened. Can be called on a 
        /// disposed <see cref="ISqlManager"/>.
        /// </summary>
        bool IsOpen();
        
        /// <summary>
        /// Tries to remove all objects from a given schema.
        /// </summary>
        /// <param name="schemaName">Name of the schema. Must not be null nor empty.</param>
        /// <param name="dropSchema">True to drop the schema itself.</param>
        /// <returns>True on success, false otherwise.</returns>
        bool SchemaDropAllObjects( string schemaName, bool dropSchema );

        /// <summary>
        /// Simple execute scalar helper.
        /// The connection must be opened.
        /// </summary>
        /// <typeparam name="T">Type to retrieve.</typeparam>
        /// <param name="select">Select clause.</param>
        /// <returns>The scalar (may be DBNull.Value) or null if no result has been returned.</returns>
        object ExecuteScalar( string select );

        /// <summary>
        /// Simple execute helper.
        /// The connection must be opened.
        /// </summary>
        /// <param name="cmd">The command text.</param>
        /// <returns>The number of rows.</returns>
        int ExecuteNonQuery( string cmd, int timeoutSecond = -1 );
    }
}
