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
    /// one connection instead of creating new ones.
    /// </summary>
    public class SqlConnectionProvider : IDisposable
    {
        /// <summary>
        /// The connection string must be kept as is because <see cref="SqlConnection.ConnectionString"/>
        /// may be modified by the SqlConnection (security information are removed).
        /// </summary>
        string			_strConn;
        SqlConnection	_oCon;
        bool			_oConIsWorking;
        bool			_autoClose;

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
        /// Should be used for read access to properties only.
        /// </summary>
        public SqlConnection InternalConnection
        {
            get { return _oCon; }
        }

        /// <summary>
        /// Gets or sets the connection string. If the main connection is currently opened
        /// (and the connection string actually changed), it is automatically closed first (and 
        /// remains closed until the first time it is needed).
        /// </summary>
        public string ConnectionString
        {
            get { return _strConn; }
            set
            {
                if( _strConn != value )
                {
                    _oCon.Close();
                    _oCon.ConnectionString = value;
                    _strConn = value;
                }
            }
        }

        /// <summary>
        /// Closes the connection to the database if it were opened. Can be called safely 
        /// multiple times.
        /// </summary>
        public void Close()
        {
            _oCon.Close();
        }

        /// <summary>
        /// Closes the connection to the database if it were opened. Can be called safely 
        /// multiple times.
        /// </summary>
        /// <remarks>
        /// You can reuse this <see cref="SqlConnectionProvider"/> even if <b>Dispose</b> has been 
        /// called.
        /// </remarks>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Open the main connection to the database if it were closed (does nothing if the 
        /// <see cref="SqlConnection"/> were already opened). Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="Close"/> is made.
        /// </summary>
        /// <remarks>Once directly opened with this method,
        /// the <see cref="KeepOpened"/> parameter is ignored: the connection will remain opened
        /// until an explicit call to <see cref="Close"/> is made.
        /// </remarks>
        public void Open()
        {
            if( _oCon.State == ConnectionState.Closed ) _oCon.Open();
        }

        /// <summary>
        /// Gets or sets wether the main connection will be reused as much as possible.
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
        /// -1 to use the default value, Caution: 0 waits indefinitly (see <see cref="SqlCommand.CommandTimeout"/>).
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
            SqlDataReader r = null;
            try
            {
                bool mustClose;
                AcquireConnection( cmd, out mustClose );
                CommandBehavior options = mustClose ? (CommandBehavior.SingleRow | CommandBehavior.CloseConnection) : CommandBehavior.SingleRow;
                r = cmd.ExecuteReader( options );
                if( !r.Read() ) return null;
                object[] res = new object[r.FieldCount];
                r.GetValues( res );
                return res;
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
            finally
            {
                ReleaseReader( r, cmd );
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
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The string to execute.</param>
        /// <param name="timeoutSecond">
        /// The maximum number of seconds to wait for the result. 
        /// -1 to use the default value, Caution: 0 waits indefinitly (see <see cref="SqlCommand.CommandTimeout"/>).
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
            bool mustClose;
            AcquireConnection( cmd, out mustClose );
            try
            {
                return cmd.ExecuteNonQuery();
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
            finally
            {
                ReleaseConnection( cmd, mustClose );
            }
        }

        /// <summary>
        /// Executes the command on the main shared connection if possible and, if the 
        /// main connection is in use, acquires a new connection.
        /// </summary>
        /// <param name="cmd">The select command to execute.</param>
        /// <param name="timeoutSecond">The maximum number of seconds to wait for the result. 
        /// -1 to use the default value, Caution: 0 waits indefinitly (see <see cref="SqlCommand.CommandTimeout"/>).</param>
        /// <returns>The first column of the first row in the resultset.</returns>
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
        /// <returns>The first column of the first row in the resultset.</returns>
        /// <remarks>
        /// Exceptions are not caught by this method: acquired resources will be 
        /// correctly released but exceptions will be propagated to caller.
        /// </remarks>
        public object ExecuteScalar( SqlCommand cmd )
        {
            bool mustClose;
            AcquireConnection( cmd, out mustClose );
            try
            {
                return cmd.ExecuteScalar();
            }
            catch( SqlException ex )
            {
                throw SqlDetailedException.Create( cmd, ex );
            }
            finally
            {
                ReleaseConnection( cmd, mustClose );
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
            bool mustClose;
            AcquireConnection( cmd, out mustClose );
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
                ReleaseConnection( cmd, mustClose );
            }
        }

        /// <summary>
        /// Executes the <see cref="SqlCommand.ExecuteReader(CommandBehavior)"/> if possible on the shared 
        /// connection. In any case <see cref="ReleaseReader"/> must be called.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The <see cref="SqlCommand"/> to execute.</param>
        /// <returns>A <see cref="SqlDataReader"/> object.</returns>
        /// <remarks>
        /// Use this method with this call pattern (suppose that a 
        /// <see cref="SqlConnectionProvider"/> named <c>_sqlProvider</c> and a <see cref="SqlCommand"/>
        /// named <c>cmd</c> exist):
        /// <code>
        /// IDataReader r = _sqlProvider.AcquireReader( cmd );
        /// try
        /// {
        ///		while( r.Read() )
        ///		{
        ///			// Process the returned data
        ///		}
        ///	}
        ///	finally
        ///	{
        ///		_sqlProvider.ReleaseReader( r, cmd );
        ///	}
        /// </code>
        /// </remarks>
        public IDataReader AcquireReader( SqlCommand cmd )
        {
            bool mustClose;
            AcquireConnection( cmd, out mustClose );
            return mustClose ? cmd.ExecuteReader( CommandBehavior.CloseConnection ) : cmd.ExecuteReader();
        }

        /// <summary>
        /// Calls <see cref="AcquireReader(SqlCommand)"/> with the <see cref="ISqlCommandHolder.Command"/> property.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The <see cref="ISqlCommandHolder"/> which <b>Command</b> must be executed.</param>
        /// <param name="mustClose">
        /// Output parameters returned by <see cref="AcquireConnection( SqlCommand, bool)"/> which states 
        /// whether the connection must be closed when the reader is released.
        /// </param>
        /// <returns>A <see cref="XmlReader"/> which holds read xml data.</returns>
        public XmlReader AcquireXmlReader( SqlCommand cmd, out bool mustClose )
        {
            AcquireConnection( cmd, out mustClose );
            return cmd.ExecuteXmlReader();
        }

        /// <summary>
        /// Releases a <see cref="IDataReader"/> previously obtained with <see cref="AcquireReader"/>.
        /// </summary>
        /// <param name="r">The reader to release: it will be closed if not null.</param>
        /// <param name="cmd">The <see cref="SqlCommand"/> to release (will not be disposed). </param>
        public void ReleaseReader( IDataReader r, SqlCommand cmd )
        {
            if( r != null ) r.Close();
            ReleaseConnection( cmd, false );
        }

        /// <summary>
        /// Releases a <see cref="XmlReader"/> previously obtained with <see cref="AcquireXmlReader"/>.
        /// </summary>
        /// <param name="r">The xmlReader to release: it will be closed if not null.</param>
        /// <param name="cmd">The <see cref="SqlCommand"/> to release (will not be disposed). </param>
        /// <param name="mustClose">Defines whether the connection must be closed or not.</param>
        public void ReleaseXmlReader( XmlReader r, SqlCommand cmd, bool mustClose )
        {
            if( r != null ) r.Close();
            ReleaseConnection( cmd, mustClose );
        }

        /// <summary>
        /// Acquires a connection. <see cref="ReleaseConnection"/> MUST be called when done.
        /// If possible, use the methods that encapsulates handles management (methods named ExecuteXXX or ReadXXX) 
        /// rather that AcquireXXX methods like this one.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="mustClose">
        /// States whether the connection used to execute the command must be closed or not.
        /// Pass it as-is to <see cref="ReleaseConnection"/>.
        /// </param>
        public void AcquireConnection( SqlCommand cmd, out bool mustClose )
        {
            if( cmd == null ) throw new ArgumentNullException( "cmd" );
            if( cmd.Connection == null )
            {
                cmd.Connection = AcquireConn( out mustClose );
            }
            else mustClose = false;
        }

        /// <summary>
        /// Releases a conccetion previously aquired by a call to <see cref="AcquireConnection"/>.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="mustClose">Value obtained by <see cref="AcquireConnection"/>.<see cref="ReleaseConnection"/>.
        /// </param>
        public void ReleaseConnection( SqlCommand cmd, bool mustClose )
        {
            if( mustClose ) cmd.Connection.Close();
            
            if( cmd.Connection == _oCon )
            {
                _oConIsWorking = false;
                cmd.Connection = null;
            }
            
            if( mustClose ) cmd.Connection = null;
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

    }
}

