#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Core\SqlDetailedException.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.IO;
using System.Data.SqlClient;
using System.Runtime.Serialization;

namespace CK.SqlServer
{
    /// <summary>
    /// Wraps a <see cref="SqlException"/> and offers a detailed 
    /// message with the actual parameter values.
    /// </summary>
    [Serializable]
    public class SqlDetailedException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="SqlDetailedException"/> on an inner <see cref="SqlException"/>.
        /// </summary>
        /// <param name="message">Message for this exception.</param>
        /// <param name="ex">Inner exception.</param>
        public SqlDetailedException( string message, SqlException ex )
            : base( message, ex )
        {
        }

        /// <summary>
        /// Serialization support.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="c">The context.</param>
        protected SqlDetailedException( SerializationInfo info, StreamingContext c )
            : base( info, c )
        {
        }

        /// <summary>
        /// Creates a new SqlDetailedException with a message containing the call.
        /// </summary>
        /// <param name="cmd">Command that generated the exception.</param>
        /// <param name="ex">The exception itself.</param>
        /// <returns></returns>
        static public SqlDetailedException Create( SqlCommand cmd, SqlException ex )
        {
            return new SqlDetailedException( SqlHelper.CommandAsText( cmd ), ex );
        }

        /// <summary>
        /// Executes a lambda and transforms any <see cref="SqlException"/> into a <see cref="SqlDetailedException"/>
        /// thrown by the action.
        /// </summary>
        /// <param name="cmd">The command to execute.</param>
        /// <param name="action">The action that executes the command.</param>
        static public void Catch( SqlCommand cmd, Action<SqlCommand> action )
        {
            try
            {
                action( cmd );
            }
            catch( SqlException ex )
            {
                throw Create( cmd, ex );
            }
        }

        /// <summary>
        /// Gets the <see cref="SqlException.Number"/>.
        /// </summary>
        public int Number
        {
            get { return InnerException.Number; }
        }

        /// <summary>
        /// Gets the inner exception that is necessarily a <see cref="SqlException"/>.
        /// </summary>
        public new SqlException InnerException
        {
            get { return (SqlException)base.InnerException; }
        }

    }
}
