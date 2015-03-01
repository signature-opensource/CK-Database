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
using System.Threading.Tasks;

namespace CK.SqlServer
{
    public partial class SqlConnectionProvider
    {
        /// <summary>
        /// Open the main connection in async fashion way to the database if it were closed (does nothing if the 
        /// <see cref="SqlConnection"/> were already opened). Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="Close"/> is made.
        /// </summary>
        /// <remarks>Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="Close"/> is made.
        /// </remarks>
        public async Task OpenAsync()
        {
            if( _oCon.State == ConnectionState.Closed ) await _oCon.OpenAsync();
        }

        /// <summary>
        /// Acquires a connection.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <returns>States whether the connection used to execute the command must be closed or not.</returns>
        public async Task<IDisposable> AcquireConnectionAsync( SqlCommand cmd )
        {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            bool mustClose;
            if( cmd.Connection == null )
            {
                var result = await AcquireConnAsync();
                cmd.Connection = result.Item1;
                mustClose = result.Item2;
            }
            else mustClose = false;
            return new SqlConnectionProviderAsyncDisposable( cmd, mustClose, this );
        }

        async Task<Tuple<SqlConnection, bool>> AcquireConnAsync()
        {
            bool mustClose = false;
            if( _oConIsWorking )
            {
                SqlConnection c = new SqlConnection( _strConn );
                await c.OpenAsync();
                mustClose = true;
                return new Tuple<SqlConnection, bool>( c, mustClose );
            }
            if( _oCon.State == ConnectionState.Closed )
            {
                await _oCon.OpenAsync();
                mustClose = _autoClose;
            }
            else mustClose = false;
            _oConIsWorking = true;
            return new Tuple<SqlConnection, bool>( _oCon, mustClose );
        }

        class SqlConnectionProviderAsyncDisposable : IDisposable
        {
            SqlCommand _cmd;
            bool _mustClose;
            SqlConnectionProvider _p;

            public SqlConnectionProviderAsyncDisposable( SqlCommand cmd, bool mustClose, SqlConnectionProvider p )
            {
                _cmd = cmd;
                _mustClose = mustClose;
                _p = p;
            }

            public void Dispose()
            {
                if( _mustClose ) _cmd.Connection.Close();

                if( _cmd.Connection == _p._oCon )
                {
                    _p._oConIsWorking = false;
                }
            }
        }

    }
}
