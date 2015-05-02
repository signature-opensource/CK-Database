using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup
{
    /// <summary>
    /// Standard implementation of a disposable <see cref="ISqlCallContext"/> that supports 
    /// query execution.
    /// This is the simplest way to implement calls to the database: by specializing this type, generic contextual properties
    /// (like ActorId) can also be used to automatically set method parameter values.
    /// </summary>
    public class SqlStandardCallContext : ISqlCallContext, IDisposable
    {
        object _cache;

        /// <summary>
        /// Finds or creates a cached <see cref="SqlConnectionProvider"/>. 
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A new or already existing <see cref="SqlConnectionProvider"/>.</returns>
        public SqlConnectionProvider GetProvider( string connectionString )
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
        /// Disposes any cached <see cref="SqlConnectionProvider"/>: this <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
        /// </summary>
        public void Dispose()
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

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="cmd">The command to execute.</param>
        public void ExecuteNonQuery( string connectionString, SqlCommand cmd )
        {
            var c = GetProvider( connectionString );
            c.ExecuteNonQuery( cmd );
        }
    }
}
