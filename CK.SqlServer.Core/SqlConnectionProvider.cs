#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Core\SqlConnectionProvider.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Xml;

namespace CK.SqlServer
{
    /// <summary>
    /// Offers methods such as ExecuteNonQuery and ExecuteScalar that safely reuse 
    /// one connection (see <see cref="KeepOpened"/> that is true by default) instead of creating new ones.
    /// This object is resilient to multiple dispose/<see cref="ExplicitClose"/> and <see cref="ExplicitOpen"/> calls: the 
    /// <see cref="InternalConnection"/> is opened/closed as needed.
    /// </summary>
    public partial class SqlConnectionProvider : IDisposable
    {
        /// <summary>
        /// The connection string must be kept as is because <see cref="SqlConnection.ConnectionString"/>
        /// may be modified by the SqlConnection (security information are removed).
        /// </summary>
        readonly string			_strConn;
        readonly SqlConnection	_oCon;
        int                     _explicitOpen;
        bool			        _oConIsWorking;
        bool			        _autoClose;

        /// <summary>
        /// Initializes a new unitialized instance. 
        /// <see cref="KeepOpened"/> is true by default.
        /// <see cref="ConnectionString"/> is null.
        /// </summary>
        public SqlConnectionProvider()
            : this( null, true )
        {
        }

        /// <summary>
        /// Initializes a new instance (do not automatically attempt to connect to the database).
        /// </summary>
        /// <param name="connectionString">The <see cref="SqlConnection.ConnectionString"/> to the
        /// database.</param>
        /// <param name="keepOpened">
        /// When sets to true (the default), once the connection has 
        /// been opened, it is kept opened and reused whenever possible.
        /// </param>
        public SqlConnectionProvider( string connectionString, bool keepOpened = true )
        {
            _strConn = connectionString;
            _oCon = new SqlConnection( connectionString );
            _autoClose = !keepOpened;
        }

        /// <summary>
        /// Gets the main <see cref="SqlConnection"/> for this <see cref="SqlConnectionProvider"/>. 
        /// This connection is not necessarily opened.
        /// </summary>
        public SqlConnection InternalConnection
        {
            get { return _oCon; }
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        public string ConnectionString
        {
            get { return _strConn; }
        }

        /// <summary>
        /// Opens the main connection to the database if it were closed (only increments <see cref="ExplicitOpenCount"/> if the 
        /// <see cref="SqlConnection"/> were already opened). Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until a corresponding explicit call to <see cref="ExplicitClose"/> is made.
        /// </summary>
        /// <remarks>
        /// Once directly opened with this method, the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="ExplicitClose"/> is made.
        /// </remarks>
        public void ExplicitOpen()
        {
            ++_explicitOpen;
            if( _oCon.State == ConnectionState.Open ) return;
            if( _oCon.State == ConnectionState.Broken ) _oCon.Close();
            if( _oCon.State == ConnectionState.Closed ) _oCon.Open();
        }
        
        /// <summary>
        /// Gets the current number of <see cref="ExplicitOpen"/>.
        /// </summary>
        public int ExplicitOpenCount
        {
            get { return _explicitOpen; }
        }

        /// <summary>
        /// Closes the connection to the database: decrements <see cref="ExplicitOpenCount"/> and closes the connection if it is zero
        /// and if no connection acquired by <see cref="AcquireConnection"/> are pending (ie. not disposed).
        /// Can be called safely multiple times.
        /// </summary>
        public void ExplicitClose()
        {
            if( _explicitOpen != 0 )
            {
                if( --_explicitOpen == 0 && !_oConIsWorking ) _oCon.Close();
            }
        }

        /// <summary>
        /// Closes the connection to the database regardless of the number of times <see cref="ExplicitOpen"/> has been called
        /// or wether connection acquired by <see cref="AcquireConnection"/> are pending.
        /// Can be called safely multiple times.
        /// </summary>
        public void Dispose()
        {
            _oCon.Close();
            _explicitOpen = 0;
        }


        /// <summary>
        /// Gets or sets whether the main connection will be reused as much as possible.
        /// Defaults to true.
        /// </summary>
        public bool KeepOpened
        {
            get { return !_autoClose; }
            set { _autoClose = !value; }
        }

        /// <summary>
        /// Executes the command and returns the first row as an array of object values.
        /// </summary>
        /// <param name="cmd">The string to execute.</param>
        /// <param name="timeoutSecond">
        /// The maximum number of seconds to wait for the result. 
        /// -1 to use the default value, Caution: 0 waits indefinitely (see <see cref="SqlCommand.CommandTimeout"/>).
        /// </param>
        /// <returns>An array of objects or null if nothing has been returned from database.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public object[] ReadFirstRow( string cmd, int timeoutSecond = -1 )
        {
            using( var c = new SqlCommand( cmd ) )
            {
                if( timeoutSecond >= 0 ) c.CommandTimeout = timeoutSecond;
                return ReadFirstRow( c );
            }
        }

        /// <summary>
        /// Executes the command and returns the first row as an array of object values.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>An array of objects or null if nothing has been returned from database.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public object[] ReadFirstRow( SqlCommand cmd )
        {
            using( AcquireConnection( cmd ) )
            {
                using( SqlDataReader r = cmd.ExecuteReader( CommandBehavior.SingleRow ) )
                {
                    try
                    {
                        if( !r.Read() ) return null;
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
        /// Creates a new connection to the database and calls <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/>
        /// with <see cref="CommandBehavior.CloseConnection"/> option.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>The newly created <see cref="SqlDataReader"/>.</returns>
        /// <remarks>
        /// Since the <see cref="SqlDataReader"/> is out of control once returned, this
        /// method does not try to share the main connection. Instead, it implements the most 
        /// secure way of handling this case: the connection is autonomous and will be automatically
        /// closed when the <see cref="SqlDataReader"/> will be closed.
        /// </remarks>
        [Obsolete( "Fixed spelling, use ExecuteIndependentReader", true )]
        public SqlDataReader ExecuteIndependantReader( SqlCommand cmd )
        {
            try
            {
                cmd.Connection = new SqlConnection( _strConn );
                cmd.Connection.Open();
                return cmd.ExecuteReader( CommandBehavior.CloseConnection );
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
        }


        /// <summary>
        /// Creates a new connection to the database and calls <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/>
        /// with <see cref="CommandBehavior.CloseConnection"/> option.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>The newly created <see cref="SqlDataReader"/>.</returns>
        /// <remarks>
        /// Since the <see cref="SqlDataReader"/> is out of control once returned, this
        /// method does not try to share the main connection. Instead, it implements the most 
        /// secure way of handling this case: the connection is autonomous and will be automatically
        /// closed when the <see cref="SqlDataReader"/> will be closed.
        /// </remarks>
        public SqlDataReader ExecuteIndependentReader( SqlCommand cmd )
        {
            try
            {
                cmd.Connection = new SqlConnection( _strConn );
                cmd.Connection.Open();
                return cmd.ExecuteReader( CommandBehavior.CloseConnection );
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The string to execute.</param>
        /// <param name="timeoutSecond">
        /// The maximum number of seconds to wait for the result. 
        /// -1 to use the default value, Caution: 0 waits indefinitely (see <see cref="SqlCommand.CommandTimeout"/>).
        /// </param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public int ExecuteNonQuery( string cmd, int timeoutSecond = -1 )
        {
            using( var c = new SqlCommand( cmd ) )
            {
                if( timeoutSecond >= 0 ) c.CommandTimeout = timeoutSecond;
                return ExecuteNonQuery( c );
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// Throws <see cref="SqlDetailedException"/> instead of <see cref="SqlException"/>.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// Exceptions are propagated to caller wrapped in a new <see cref="SqlDetailedException"/>
        /// for which message is the executed text (stored procedure calls are automatically 
        /// expanded with the actual parameters value).<br/>
        /// To get the <see cref="SqlException"/>, use the <see cref="SqlDetailedException.InnerException"/> property.
        /// </remarks>
        public int ExecuteNonQuery( SqlCommand cmd )
        {
            using( AcquireConnection( cmd ) )
            {
                try
                {
                    return cmd.ExecuteNonQuery();
                }
                catch( SqlException ex )
                {
                    throw SqlDetailedException.Create( cmd, ex );
                }
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The select command to execute.</param>
        /// <param name="timeoutSecond">The maximum number of seconds to wait for the result. 
        /// -1 to use the default value, Caution: 0 waits indefinitely (see <see cref="SqlCommand.CommandTimeout"/>).</param>
        /// <returns>The first column of the first row in the result set.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public object ExecuteScalar( string cmd, int timeoutSecond = -1 )
        {
            using( var c = new SqlCommand( cmd ) )
            {
                if( timeoutSecond >= 0 ) c.CommandTimeout = timeoutSecond;
                return ExecuteScalar( c );
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public object ExecuteScalar( SqlCommand cmd )
        {
            using( AcquireConnection( cmd ) )
            {
                try
                {
                    return cmd.ExecuteScalar();
                }
                catch( SqlException ex )
                {
                    throw SqlDetailedException.Create( cmd, ex );
                }
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <param name="processor">A function which knows how to process the <see cref="XmlReader"/> read from the command.</param>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public void ExecuteXmlReader( SqlCommand cmd, Action<XmlReader> processor )
        {
            using( AcquireConnection( cmd ) )
            {
                XmlReader r = null;
                try
                {
                    r = cmd.ExecuteXmlReader();
                    processor( r );
                }
                catch( SqlException ex )
                {
                    throw SqlDetailedException.Create( cmd, ex );
                }
                finally
                {
                    if( r != null ) r.Close();
                }
            }
        }

        /// <summary>
        /// Executes the <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/> if possible on the shared 
        /// connection.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>A <see cref="CKDataReader"/> object.</returns>
        public CKDataReader AcquireReader( SqlCommand cmd )
        {
            IDisposable connection = AcquireConnection( cmd );
            return new CKDataReader( cmd.ExecuteReader(), connection );
        }

        /// <summary>
        /// Acquires a connection.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        public IDisposable AcquireConnection( SqlCommand cmd )
        {
            bool mustClose;
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( cmd.Connection == null )
            {
                cmd.Connection = AcquireConn( out mustClose );
            }
            else mustClose = false;
            return new SqlConnectionProviderDisposable( cmd, mustClose, this );
        }

        SqlConnection AcquireConn( out bool mustClose )
        {
            if( _oConIsWorking )
            {
                SqlConnection c = new SqlConnection( _strConn );
                c.Open();
                mustClose = true;
                return c;
            }
            if( _oCon.State == ConnectionState.Closed )
            {
                _oCon.Open();
                mustClose = _autoClose;
            }
            else mustClose = false;
            _oConIsWorking = true;
            return _oCon;
        }

        internal class SqlConnectionProviderDisposable : IDisposable
        {
            readonly SqlCommand _cmd;
            readonly bool _mustClose;
            readonly SqlConnectionProvider _p;

            public SqlConnectionProviderDisposable( SqlCommand cmd, bool mustClose, SqlConnectionProvider p )
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
                    _cmd.Connection = null;
                }

                if( _mustClose ) _cmd.Connection = null;
            }
        }

    }
}

