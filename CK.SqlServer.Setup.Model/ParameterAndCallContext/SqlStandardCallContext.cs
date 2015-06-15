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
    /// Standard implementation of a disposable <see cref="ISqlParameterContext"/> that supports 
    /// query execution by explicitely implementing <see cref="ISqlCommandExecutor"/>).
    /// This is the simplest way to implement calls to the database: by specializing this type, application specific
    /// properties (like the ActorId) can also be used to automatically set method parameter values.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class directly implements <see cref="ISqlCommandExecutor"/> interface but with explicit methods in order to avoid interface pollution
    /// when this object exposes parameter values (it is a <see cref="ISqlParameterContext"/>) and also to avoid creating any extra object.
    /// </para>
    /// <para>
    /// The <see cref="SqlConnectionProvider"/> that are created by <see cref="ISqlCommandExecutor.GetProvider"/> are cached
    /// and reused until <see cref="Dispose"/> is called.
    /// </para>
    /// </remarks>
    public class SqlStandardCallContext : IDisposableSqlCallContext, ISqlCommandExecutor
    {
        object _cache;

        /// <summary>
        /// Disposes any cached <see cref="SqlConnectionProvider"/>: this <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
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

        ISqlCommandExecutor ISqlCallContext.Executor
        {
            get { return this; }
        }

        SqlConnectionProvider ISqlCommandExecutor.GetProvider( string connectionString )
        {
            return GetProvider( connectionString );
        }

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

        void ISqlCommandExecutor.ExecuteNonQuery( string connectionString, SqlCommand cmd )
        {
            GetProvider( connectionString ).ExecuteNonQuery( cmd );
        }

        Task ISqlCommandExecutor.ExecuteNonQueryAsync( string connectionString, SqlCommand cmd )
        {
            return ExecAsync<string>( connectionString, cmd, _ => null );
        }

        Task ISqlCommandExecutor.ExecuteNonQueryAsyncCancellable( string connectionString, SqlCommand cmd, CancellationToken cancellationToken )
        {
            return ExecAsync<string>( connectionString, cmd, _ => null, cancellationToken );
        }

        Task<T> ISqlCommandExecutor.ExecuteNonQueryAsyncTyped<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder )
        {
            return ExecAsync<T>( connectionString, cmd, resultBuilder );
        }

        Task<T> ISqlCommandExecutor.ExecuteNonQueryAsyncTypedCancellable<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken )
        {
            return ExecAsync<T>( connectionString, cmd, resultBuilder, cancellationToken );
        }

        Task<T> ExecAsync<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken = default(CancellationToken) )
        {
            var tcs = new TaskCompletionSource<T>();

            Task<IDisposable> openTask = GetProvider( connectionString ).AcquireConnectionAsync( cmd, cancellationToken );
            openTask
                .ContinueWith( open =>
                    {
                        if( open.IsFaulted ) tcs.SetException( open.Exception.InnerExceptions );
                        else if( open.IsCanceled ) tcs.SetCanceled();
                        else
                        {
                            try
                            {
                                var execTask = cmd.ExecuteNonQueryAsync( cancellationToken );
                                execTask.ContinueWith( exec =>
                                {
                                    if( exec.IsFaulted ) tcs.SetException( exec.Exception.InnerExceptions );
                                    else if( exec.IsCanceled ) tcs.SetCanceled();
                                    else
                                    {
                                        tcs.SetResult( resultBuilder( cmd ) );
                                    }
                                }, TaskContinuationOptions.ExecuteSynchronously );
                            }
                            catch( Exception exc ) { tcs.TrySetException( exc ); }
                        }
                    }, TaskContinuationOptions.ExecuteSynchronously )
                .ContinueWith( _ =>
                    {
                        if( !openTask.IsFaulted ) openTask.Result.Dispose();
                    }, TaskContinuationOptions.ExecuteSynchronously );

            return tcs.Task;
        }
    }
}
