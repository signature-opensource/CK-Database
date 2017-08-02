using CK.SqlServer.Setup;
using System;
using System.Collections.Generic;
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
        /// <param name="cancel">Optional cancellation token.</param>
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

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Returned type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="defaultValue">Default value to use if no result is available.</param>
        /// <returns>The read or default value.</returns>
        public static async Task<T> ExecuteScalarAsync<T>( this SqlCommand @this, SqlConnection connection, T defaultValue = default( T ) )
        {
            try
            {
                using( await (@this.Connection = connection).EnsureOpenAsync().ConfigureAwait( false ) )
                {
                    object o = await @this.ExecuteScalarAsync().ConfigureAwait( false );
                    return o != null && o != DBNull.Value ? (T)o : defaultValue;
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a statement on an already opened or closed connection.
        /// </summary>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <returns>The awaitable.</returns>
        public static async Task ExecuteNonQueryAsync( this SqlCommand @this, SqlConnection connection )
        {
            try
            {
                using( await (@this.Connection = connection).EnsureOpenAsync().ConfigureAwait( false ) )
                {
                    await @this.ExecuteNonQueryAsync().ConfigureAwait( false );
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a on-row query (uses <see cref="CommandBehavior.SingleRow"/>) and builds an object based on
        /// the row data.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="builder">The function that builds an object: called with a null reader when there is no result.</param>
        /// <returns>The build object.</returns>
        public static async Task<T> ExecuteRowAsync<T>( this SqlCommand @this, SqlConnection connection, Func<SqlDataReader, T> builder )
        {
            try
            {
                using( await (@this.Connection = connection).EnsureOpenAsync().ConfigureAwait( false ) )
                using( var r = await @this.ExecuteReaderAsync( CommandBehavior.SingleRow ).ConfigureAwait( false ) )
                {
                    return await r.ReadAsync().ConfigureAwait( false )
                            ? builder( r )
                            : builder( null );
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a query and builds a list of objects.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="builder">The function that builds objects and add them to the collector.</param>
        /// <returns>The list of objects.</returns>
        public static async Task<List<T>> ExecuteReaderAsync<T>( this SqlCommand @this, SqlConnection connection, Action<SqlDataReader, List<T>> builder )
        {
            try
            {
                using( await (@this.Connection = connection).EnsureOpenAsync().ConfigureAwait( false ) )
                using( var r = await @this.ExecuteReaderAsync().ConfigureAwait( false ) )
                {
                    var collector = new List<T>();
                    while( await r.ReadAsync().ConfigureAwait( false ) )
                    {
                        builder( r, collector );
                    }
                    return collector;
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result
        /// set returned by the query on a closed or already opened connection. All other columns and rows are ignored.
        /// </summary>
        /// <typeparam name="T">Returned type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="defaultValue">Default value to use if no result is available.</param>
        /// <returns>The read or default value.</returns>
        public static T ExecuteScalar<T>( this SqlCommand @this, SqlConnection connection, T defaultValue = default( T ) )
        {
            try
            {
                using( (@this.Connection = connection).EnsureOpen() )
                {
                    object o = @this.ExecuteScalar();
                    return o != null && o != DBNull.Value ? (T)o : defaultValue;
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a statement on an already opened or closed connection.
        /// </summary>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        public static void ExecuteNonQuery( this SqlCommand @this, SqlConnection connection )
        {
            try
            {
                using( (@this.Connection = connection).EnsureOpen() )
                {
                    @this.ExecuteNonQuery();
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a on-row query (uses <see cref="CommandBehavior.SingleRow"/>) and builds an object based on
        /// the row data.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="builder">The function that builds an object: called with a null reader when there is no result.</param>
        /// <returns>The build object.</returns>
        public static T ExecuteRow<T>( this SqlCommand @this, SqlConnection connection, Func<SqlDataReader, T> builder )
        {
            try
            {
                using( (@this.Connection = connection).EnsureOpen() )
                using( var r = @this.ExecuteReader( CommandBehavior.SingleRow ) )
                {
                    return r.Read()
                            ? builder( r )
                            : builder( null );
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

        /// <summary>
        /// Executes a query and builds a list of objects.
        /// </summary>
        /// <typeparam name="T">The result object type.</typeparam>
        /// <param name="this">This command.</param>
        /// <param name="connection">The connection, it is automatically opened and closed if needed.</param>
        /// <param name="builder">The function that builds objects and add them to the collector.</param>
        /// <returns>The list of objects.</returns>
        public static List<T> ExecuteReader<T>( this SqlCommand @this, SqlConnection connection, Action<SqlDataReader, List<T>> builder )
        {
            try
            {
                using( (@this.Connection = connection).EnsureOpen() )
                using( var r = @this.ExecuteReader() )
                {
                    var collector = new List<T>();
                    while( r.Read() )
                    {
                        builder( r, collector );
                    }
                    return collector;
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( @this, ex );
            }
        }

    }
}
