using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;

namespace CK.SqlServer
{
    /// <summary>
    /// Standard implementation of a disposable <see cref="ISqlCallContext"/> that supports 
    /// query execution by explicitely implementing <see cref="ISqlCommandExecutor"/>).
    /// This is the simplest way to implement calls to the database: by specializing this type, application specific
    /// properties (like the ActorId) can also be used to automatically set method parameter values when used 
    /// with a <see cref="CK.SqlServer.Setup.ParameterSourceAttribute"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class directly implements <see cref="ISqlCommandExecutor"/> interface but with explicit methods in order to avoid interface pollution
    /// when this object exposes parameter values.
    /// </para>
    /// <para>
    /// The <see cref="ISqlConnectionController"/> that are created by <see cref="ISqlCommandExecutor.GetProvider"/> are cached
    /// and reused until <see cref="Dispose"/> is called.
    /// </para>
    /// </remarks>
    public class SqlStandardCallContext : IDisposableSqlCallContext, ISqlCommandExecutor
    {
        object _cache;

        ISqlCommandExecutor ISqlCallContext.Executor => this; 

        [Obsolete()]
        SqlConnectionProvider ISqlCommandExecutor.GetProvider( string connectionString ) => GetProvider( connectionString );

#pragma warning disable 0618
        SqlConnectionProvider GetProvider( string connectionString )
        {
            SqlConnectionProvider c;
            if( _cache == null )
            {
                c = new SqlConnectionProvider( connectionString );
                _cache = c;
                return c;
            }
            SqlConnectionProvider newC;
            c = _cache as SqlConnectionProvider;
            if( c != null )
            {
                if( c.ConnectionString == connectionString ) return c;
                newC = new SqlConnectionProvider( connectionString );
                _cache = new SqlConnectionProvider[] { c, newC };
            }
            else
            {
                SqlConnectionProvider[] cache = (SqlConnectionProvider[])_cache;
                for( int i = 0; i < cache.Length; i++ )
                {
                    c = cache[i];
                    if( c.ConnectionString == connectionString ) return c;
                }
                SqlConnectionProvider[] newCache = new SqlConnectionProvider[cache.Length + 1];
                Array.Copy( cache, newCache, cache.Length );
                newC = new SqlConnectionProvider( connectionString );
                newCache[cache.Length] = newC;
                _cache = newCache;
            }
            return newC;
        }

        /// <summary>
        /// Disposes any cached <see cref="SqlConnection"/>: this <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
        /// </summary>
        public virtual void Dispose()
        {
            if( _cache != null )
            {
                SqlConnectionProvider c = _cache as SqlConnectionProvider;
                if( c != null ) c.Dispose();
                else
                {
                    SqlConnectionProvider[] cache = _cache as SqlConnectionProvider[];
                    for( int i = 0; i < cache.Length; ++i ) cache[i].Dispose();
                }
                _cache = null;
            }
        }

#pragma warning restore 0618

        /// <summary>
        /// Gets the connection to use for a given connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SqlConnection to use that may be already opened.</returns>
        public SqlConnection this[string connectionString] => GetProvider( connectionString ).Connection;

        /// <summary>
        /// Gets the connection to use given a connection string provider.
        /// </summary>
        /// <param name="p">The provider of the connection string.</param>
        /// <returns>The SqlConnection to use that may be already opened.</returns>
        public SqlConnection this[ISqlConnectionStringProvider p] => GetProvider( p.ConnectionString ).Connection;

        /// <summary>
        /// Gets the contoller of a connection that can be use to pre open 
        /// the connection instead of relying on local open/close.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The controller for the connection.</returns>
        public ISqlConnectionController GetConnectionController( string connectionString ) => GetProvider( connectionString );

        void ISqlCommandExecutor.ExecuteNonQuery( string connectionString, SqlCommand cmd )
        {
            try
            {
                var c = GetConnectionController( connectionString ).Connection;
                using( c.EnsureOpen() )
                {
                    cmd.Connection = c;
                    cmd.ExecuteNonQuery();
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
        }

        Task ISqlCommandExecutor.ExecuteNonQueryAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken )
        {
            // Trick: Reuse ExecAsync with a string type and a fake result builder using the fact that Task<T> is a Task.
            return ExecAsync<string>( connectionString, cmd, _ => null, cancellationToken );
        }

        Task<T> ISqlCommandExecutor.ExecuteNonQueryAsyncTyped<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken )
        {
            return ExecAsync( connectionString, cmd, resultBuilder, cancellationToken );
        }

        async Task<T> ExecAsync<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken = default( CancellationToken ) )
        {
            try
            {
                SqlConnection c = GetProvider( connectionString ).Connection;
                using( await c.EnsureOpenAsync( cancellationToken ).ConfigureAwait( false ) )
                {
                    cmd.Connection = c;
                    await cmd.ExecuteNonQueryAsync( cancellationToken ).ConfigureAwait( false );
                    return resultBuilder( cmd );
                }
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
        }

    }
}
