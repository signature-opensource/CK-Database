using CK.SqlServer.Setup;
using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    /// <summary>
    /// Extension methods for SqlConnection and ISqlCallContext.
    /// </summary>
    public static class SqlConnectionExtension
    {
        sealed class AutoCloser : IDisposable
        {
            readonly DbConnection _c;

            public AutoCloser( DbConnection c )
            {
                _c = c;   
            }

            public void Dispose()
            {
                _c.Close();
            }
        }

        /// <summary>
        /// Helper to be used in a using statement to open the connection if it not already opened.
        /// Returns either a IDisposable that will close it or null it the connection was already opened.
        /// </summary>
        /// <param name="this">This connection.</param>
        /// <returns>A IDisposable or null.</returns>
        public static IDisposable EnsureOpen( this DbConnection @this )
        {
            if( @this.State == ConnectionState.Closed )
            {
                @this.Open();
                return new AutoCloser( @this );
            }
            return null;
        }

        /// <summary>
        /// Helper to be used in a using statement to open the connection if it not already opened.
        /// Returns either a IDisposable that will close it or null it the connection was already opened.
        /// </summary>
        /// <param name="this">This connection.</param>
        /// <returns>A IDisposable or null.</returns>
        public static async Task<IDisposable> EnsureOpenAsync( this DbConnection @this, CancellationToken cancel = default(CancellationToken) )
        {
            if( @this.State == ConnectionState.Closed )
            {
                await @this.OpenAsync( cancel ).ConfigureAwait( false );
                return new AutoCloser( @this );
            }
            return null;
        }

        /// <summary>
        /// Gets the connection for any <see cref="ISqlConnectionStringProvider"/> object (like <see cref="SqlTable"/>
        /// or <see cref="SqlPackage"/>).
        /// This is a simple extension method that makes explicit the indexer available (<see cref="ISqlCallContext"/>) 
        /// of <see cref="ISqlCallContext"/>.
        /// </summary>
        /// <param name="this">This sql call context.</param>
        /// <param name="connectionStringProvider">The connection string provider.</param>
        /// <returns>The sql connection.</returns>
        public static SqlConnection GetConnection( this ISqlCallContext @this, ISqlConnectionStringProvider connectionStringProvider )
        {
            return @this[connectionStringProvider];
        }

        /// <summary>
        /// Gets the connection for a connection string.
        /// This is a simple extension method that makes explicit the indexer available (<see cref="ISqlCallContext"/>) 
        /// of <see cref="ISqlCallContext"/>.
        /// </summary>
        /// <param name="this">This sql call context.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The sql connection.</returns>
        public static SqlConnection GetConnection( this ISqlCallContext @this, string connectionString )
        {
            return @this[connectionString];
        }


    }
}
