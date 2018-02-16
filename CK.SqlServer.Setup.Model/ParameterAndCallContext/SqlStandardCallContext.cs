using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
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
        IActivityMonitor _monitor;
        readonly bool _ownedMonitor;

        /// <summary>
        /// Initializes a new <see cref="SqlStandardCallContext"/> that may be bound to an existing monitor.
        /// </summary>
        /// <param name="monitor">Optional monitor to use. When null, a new <see cref="ActivityMonitor"/> will be created when <see cref="Monitor"/> property is accessed.</param>
        public SqlStandardCallContext( IActivityMonitor monitor = null )
        {
            _ownedMonitor = monitor == null;
            _monitor = monitor;
        }

        /// <summary>
        /// Gets the monitor that can be used to log activities.
        /// </summary>
        public IActivityMonitor Monitor => _monitor ?? (_monitor = new ActivityMonitor());

        ISqlCommandExecutor ISqlCallContext.Executor => this;

        /// <summary>
        /// Disposes any cached <see cref="SqlConnection"/>. 
        /// This <see cref="SqlStandardCallContext"/> instance can be reused once disposed.
        /// </summary>
        public virtual void Dispose()
        {
            if( _cache != null )
            {
                Controller c = _cache as Controller;
                if( c != null ) c.Dispose();
                else
                {
                    Controller[] cache = _cache as Controller[];
                    for( int i = 0; i < cache.Length; ++i ) cache[i].Dispose();
                }
                _cache = null;
                if( _monitor != null && _ownedMonitor )
                {
                    _monitor.MonitorEnd();
                    _monitor = null;
                }
            }
        }

        class Controller : ISqlConnectionController
        {
            readonly SqlStandardCallContext _ctx;
            internal readonly string ConnectionString;
            readonly SqlConnection _connection;
            int _openCount;

            public Controller( SqlStandardCallContext ctx, string connectionString )
            {
                _ctx = ctx;
                ConnectionString = connectionString;
                _connection = new SqlConnection( connectionString );
            }

            public SqlConnection Connection => _connection;

            public int ExplicitOpenCount => _openCount;

            public void ExplicitClose()
            {
                if( _openCount > 0 )
                {
                    if( --_openCount == 0 )
                    {
                        _connection.Close();
                    }
                }
            }

            public void ExplicitOpen()
            {
                if( ++_openCount == 1 )
                {
                    _connection.Open();
                }
            }

            public Task ExplicitOpenAsync()
            {
                if( ++_openCount == 1 )
                {
                    return _connection.OpenAsync();
                }
                return Task.CompletedTask;
            }

            public void Dispose()
            {
                _connection.Dispose();
            }
        }

        Controller GetProvider( string connectionString )
        {
            Controller c;
            if( _cache == null )
            {
                c = new Controller( this, connectionString );
                _cache = c;
                return c;
            }
            Controller newC;
            c = _cache as Controller;
            if( c != null )
            {
                if( c.ConnectionString == connectionString ) return c;
                newC = new Controller( this, connectionString );
                _cache = new Controller[] { c, newC };
            }
            else
            {
                Controller[] cache = (Controller[])_cache;
                for( int i = 0; i < cache.Length; i++ )
                {
                    c = cache[i];
                    if( c.ConnectionString == connectionString ) return c;
                }
                Controller[] newCache = new Controller[cache.Length + 1];
                Array.Copy( cache, newCache, cache.Length );
                newC = new Controller( this, connectionString );
                newCache[cache.Length] = newC;
                _cache = newCache;
            }
            return newC;
        }

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

        object ISqlCommandExecutor.ExecuteScalar( string connectionString, SqlCommand cmd )
        {
            return ExecuteSync( connectionString, cmd, c => c.ExecuteScalar() );
        }

        void ISqlCommandExecutor.ExecuteNonQuery( string connectionString, SqlCommand cmd )
        {
            ExecuteSync( connectionString, cmd, c => c.ExecuteNonQuery() );
        }

        object ExecuteSync( string connectionString, SqlCommand cmd, Func<SqlCommand, object> executor )
        {
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            ISqlConnectionController c = GetConnectionController( connectionString );
            object result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    using( c.Connection.EnsureOpen() )
                    {
                        cmd.Connection = c.Connection;
                        OnCommandExecuting( cmd, retryCount );

                        result = executor( cmd );
                        break;
                    }
                }
                catch( IOException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                catch( SqlException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, c, e, previous, start );
                if( retry.Ticks < 0
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                Thread.Sleep( retry );
            }
            OnCommandExecuted( cmd, retryCount, result );
            return result;
        }

        Task ISqlCommandExecutor.ExecuteNonQueryAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken )
        {
            return ExecuteAsync( connectionString, cmd, ( c, t ) => c.ExecuteNonQueryAsync( t ), cancellationToken );
        }

        Task<object> ISqlCommandExecutor.ExecuteScalarAsync( string connectionString, SqlCommand cmd, CancellationToken cancellationToken )
        {
            return ExecuteAsync( connectionString, cmd, ( c, t ) => c.ExecuteScalarAsync( t ), cancellationToken );
        }

        async Task<object> ExecuteAsync<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, CancellationToken, Task<T>> executor, CancellationToken cancellationToken )
        {
            DateTime start = DateTime.UtcNow;
            int retryCount = 0;
            List<SqlDetailedException> previous = null;
            ISqlConnectionController c = GetConnectionController( connectionString );
            object result;
            for(; ; )
            {
                SqlDetailedException e = null;
                try
                {
                    using( await c.Connection.EnsureOpenAsync( cancellationToken ).ConfigureAwait( false ) )
                    {
                        cmd.Connection = c.Connection;
                        OnCommandExecuting( cmd, retryCount );

                        result = await executor( cmd, cancellationToken ).ConfigureAwait( false );
                        break;
                    }
                }
                catch( IOException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                catch( SqlException ex )
                {
                    e = SqlDetailedException.Create( cmd, ex, retryCount++ );
                }
                Debug.Assert( e != null );
                Monitor.Error( e );
                if( previous == null ) previous = new List<SqlDetailedException>();
                TimeSpan retry = OnCommandError( cmd, c, e, previous, start );
                if( retry.Ticks < 0
                    || retry == TimeSpan.MaxValue
                    || previous.Count > 1000 )
                {
                    throw e;
                }
                previous.Add( e );
                await Task.Delay( retry ).ConfigureAwait( false );
            }
            OnCommandExecuted( cmd, retryCount, result );
            return result;
        }

        async Task<T> ISqlCommandExecutor.ExecuteNonQueryAsyncTyped<T>( string connectionString, SqlCommand cmd, Func<SqlCommand, T> resultBuilder, CancellationToken cancellationToken )
        {
            Func<SqlCommand, CancellationToken, Task<T>> adapter;
            adapter = async ( c, t ) =>
            {
                await c.ExecuteNonQueryAsync( t );
                return resultBuilder( c );
            };
            var obj = await ExecuteAsync( connectionString, cmd, adapter, cancellationToken );
            return (T)obj;
        }

        /// <summary>
        /// Extension point called before a command is executed.
        /// </summary>
        /// <param name="cmd">The command that is ready to be executed.</param>
        /// <param name="retryNumber">Current number of retries (0 the first time).</param>
        protected virtual void OnCommandExecuting( SqlCommand cmd, int retryNumber ) { }

        /// <summary>
        /// Extension point called after a command has been successfully executed.
        /// </summary>
        /// <param name="cmd">The executed command.</param>
        /// <param name="retryCount">Number of tries before success.</param>
        /// <param name="result">
        /// The result of the <see cref="SqlCommand.ExecuteNonQuery"/> execution (number of rows),
        /// or the result of the <see cref="SqlCommand.ExecuteScalar"/>, or the .
        /// </param>
        protected virtual void OnCommandExecuted( SqlCommand cmd, int retryCount, object result ) { }

        /// <summary>
        /// Extension point called after a command failed.
        /// At this level, this method does nothing and returns <see cref="TimeSpan.MaxValue"/>: no retry will be done.
        /// <para>
        /// Note that any negative TimeSpan as well as TimeSpan.MaxValue will result in
        /// the <see cref="SqlDetailedException"/> being thrown.
        /// </para>
        /// </summary>
        /// <param name="cmd">The executing command.</param>
        /// <param name="c">The connection controller.</param>
        /// <param name="ex">The exception caught and wrapped in a <see cref="SqlDetailedException"/>.</param>
        /// <param name="previous">Previous errors when retries have been made. Empty on the first error.</param>
        /// <param name="firstExecutionTimeUtc">The Utc time of the first try.</param>
        /// <returns>The time span to retry. A negative time span or <see cref="TimeSpan.MaxValue"/> to skip retry.</returns>
        protected virtual TimeSpan OnCommandError(
            SqlCommand cmd,
            ISqlConnectionController c,
            SqlDetailedException ex,
            IReadOnlyList<SqlDetailedException> previous,
            DateTime firstExecutionTimeUtc ) => TimeSpan.MaxValue;


    }
}
