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
            readonly IDbConnection _c;

            public AutoCloser( IDbConnection c )
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
        public static IDisposable EnsureOpen( this IDbConnection @this )
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
        public static async Task<IDisposable> EnsureOpenAsync( this IDbConnection @this, CancellationToken cancel = default(CancellationToken) )
        {
            if( @this.State == ConnectionState.Closed )
            {
                await ((DbConnection)@this).OpenAsync( cancel ).ConfigureAwait( false );
                return new AutoCloser( @this );
            }
            return null;
        }


    }
}
