#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Core\SqlConnectionProvider.Async.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.SqlServer
{
    public partial class SqlConnectionProvider
    {
        /// <summary>
        /// Asynchronously opens the main connection to the database if it were closed (only increments <see cref="ExplicitOpenCount"/> if the 
        /// <see cref="SqlConnection"/> were already opened). Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until a corresponding explicit call to <see cref="ExplicitClose"/> is made.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Once directly opened with this method, the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="ExplicitClose"/> is made.
        /// </remarks>
        public Task ExplicitOpenAsync()
        {
            ++_explicitOpen;
            if( _oCon.State == ConnectionState.Open ) return Task.FromResult( 0 );
            if( _oCon.State == ConnectionState.Broken ) _oCon.Close();
            if( _oCon.State == ConnectionState.Closed ) 
            {
                return _oCon.OpenAsync();
            }
            return Task.FromResult( 0 );
        }

        /// <summary>
        /// Acquires a connection.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A task representing the asynchronous operation: a disposable object that must be disposed.</returns>
        public async Task<IDisposable> AcquireConnectionAsync( SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken) )
        {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            bool mustClose;
            if( cmd.Connection == null )
            {
                var result = await AcquireConnAsync( cancellationToken );
                cmd.Connection = result.Key;
                mustClose = result.Value;
            }
            else mustClose = false;
            return new SqlConnectionProviderDisposable( cmd, mustClose, this );
        }

        async Task<KeyValuePair<SqlConnection, bool>> AcquireConnAsync( CancellationToken cancellationToken )
        {
            bool mustClose = false;
            if( _oConIsWorking )
            {
                SqlConnection c = new SqlConnection( _strConn );
                await c.OpenAsync( cancellationToken );
                mustClose = true;
                return new KeyValuePair<SqlConnection, bool>( c, mustClose );
            }
            if( _oCon.State == ConnectionState.Closed )
            {
                await _oCon.OpenAsync( cancellationToken );
                mustClose = _autoClose;
            }
            else mustClose = false;
            _oConIsWorking = true;
            return new KeyValuePair<SqlConnection, bool>( _oCon, mustClose );
        }

        /// <summary>
        /// Executes the command and returns the first row as an array of object values.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A task representing the asynchronous operation: an array of objects or null if nothing has been returned from database.</returns>
        /// <remarks>
        /// Exceptions will be reported by the returned task object.
        /// </remarks>
        public async Task<object[]> ReadFirstRowAsync( SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken) )
        {
            using( await AcquireConnectionAsync( cmd ) )
            {
                using( SqlDataReader r = await cmd.ExecuteReaderAsync( CommandBehavior.SingleRow, cancellationToken ) ) 
                {
                    try
                    {
                        if( !await r.ReadAsync( cancellationToken ) ) return null;
                        object[] res = new object[r.FieldCount];
                        r.GetValues( res );
                        return res;
                    }
                    catch( SqlException ex )
                    {
                        throw SqlDetailedException.Create( cmd, ex );
                    }
                }
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A task representing the asynchronous operation: the first column of the first row in the result set.</returns>
        /// <remarks>
        /// Exceptions will be reported by the returned task object.
        /// </remarks>
        public async Task<object> ExecuteScalarAsync( SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken) )
        {
            using( await AcquireConnectionAsync( cmd, cancellationToken ) )
            {
                try
                {
                    return await cmd.ExecuteScalarAsync( cancellationToken );
                }
                catch( SqlException ex )
                {
                    throw SqlDetailedException.Create( cmd, ex );
                }
            }
        }

        /// <summary>
        /// Executes the non query command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns>A task representing the asynchronous operation (the number of rows affected).</returns>
        /// <remarks>
        /// Exceptions will be reported by the returned task object.
        /// </remarks>
        public async Task<int> ExecuteNonQueryAsync( SqlCommand cmd, CancellationToken cancellationToken = default(CancellationToken) )
        {
            using( await AcquireConnectionAsync( cmd, cancellationToken ) )
            {
                try
                {
                    return await cmd.ExecuteNonQueryAsync( cancellationToken );
                }
                catch( SqlException ex )
                {
                    throw SqlDetailedException.Create( cmd, ex );
                }
            }
        }

    }
}
