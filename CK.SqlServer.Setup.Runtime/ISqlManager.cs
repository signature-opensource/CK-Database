using System;
using System.Collections.Generic;
using CK.Core;
using System.Data.SqlClient;

namespace CK.SqlServer.Setup
{

    /// <summary>
    /// Extends <see cref="ISqlManagerBase"/> to offer minimal helpers
    /// to query the database.
    /// </summary>
    public interface ISqlManager : ISqlManagerBase
    {
        /// <summary>
        /// Gets the <see cref="SqlConnection"/> of this <see cref="ISqlManager"/>.
        /// </summary>
        SqlConnection Connection { get; }

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
        /// <param name="timeoutSecond">Timeout of the execution in seconds.</param>
        /// <returns>The number of rows.</returns>
        int ExecuteNonQuery( string cmd, int timeoutSecond = -1 );

        /// <summary>
        /// Executes the command and returns the first row as an array of object values.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>An array of objects or null if nothing has been returned from database.</returns>
        object[] ReadFirstRow( SqlCommand cmd );

    }
}
