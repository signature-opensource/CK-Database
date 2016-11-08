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
        /// Gets a <see cref="SqlConnectionProvider"/> for the given connection string. 
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A <see cref="SqlConnectionProvider"/>.</returns>
        [Obsolete( "Please use ISqlConnectionController and SqlConnection instead of SqlConnectionProvider." )]
        SqlConnectionProvider GetProvider( string connectionString );

        /// <summary>
        /// Gets a <see cref="ISqlConnectionController"/> for the given connection string. 
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A <see cref="ISqlConnectionController"/>.</returns>
        ISqlConnectionController GetConnectionController( string connectionString );
        
        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        void ExecuteNonQuery( string connectionString, SqlCommand cmd );

        /// <summary>
        /// Executes a command asynchrously.
        /// Can be interrupted thanks to a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task.</returns>
        Task ExecuteNonQueryAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken );

        /// <summary>
        /// Executes a command that returns a result based on the output parameters asynchrously. 
        /// Can be interrupted thanks to a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="resultBuilder">Result object builder.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The awaitable call result built from the output parameters.</returns>
        Task<T> ExecuteNonQueryAsyncTyped<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken );

    }
}
