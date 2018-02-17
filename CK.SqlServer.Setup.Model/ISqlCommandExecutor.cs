using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Defines all required methods to support command execution.
    /// </summary>
    public interface ISqlCommandExecutor
    {
        /// <summary>
        /// Executes the given command synchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="connection">The connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor.</param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        T ExecuteQuery<T>( SqlConnection connection, SqlCommand cmd, Func<SqlCommand, T> innerExecutor );

        /// <summary>
        /// Executes the given command asynchronously, relying on a function to handle the actual command
        /// execution and result construction.
        /// </summary>
        /// <typeparam name="T">Type of the returned object.</typeparam>
        /// <param name="connection">The connection controller.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="innerExecutor">The actual executor (asynchronous).</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The result of the call built by <paramref name="innerExecutor"/>.</returns>
        Task<T> ExecuteQueryAsync<T>( SqlConnection connection, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> innerExecutor, CancellationToken cancellationToken );

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>The return of the <see cref="SqlCommand.ExecuteNonQuery"/> (number of rows affected).</returns>
        int ExecuteNonQuery( string connectionString, SqlCommand cmd );

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection.
        /// All other columns and rows are ignored.
        /// The returned object is null if no rows are returned.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>The read value (can be <see cref="DBNull.Value"/>) or null if no rows are returned.</returns>
        object ExecuteScalar( string connectionString, SqlCommand cmd );

        /// <summary>
        /// Executes a command asynchrously.
        /// Can be interrupted thanks to a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The return of the <see cref="SqlCommand.ExecuteNonQuery"/> (number of rows affected).</returns>
        Task<int> ExecuteNonQueryAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken );

        /// <summary>
        /// Executes the query asynchrously and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection.
        /// All other columns and rows are ignored.
        /// The returned object is null if no rows are returned.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The read value (can be <see cref="DBNull.Value"/>) or null if no rows are returned.</returns>
        Task<object> ExecuteScalarAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken );

    }
}
