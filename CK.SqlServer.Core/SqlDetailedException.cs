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
        public SqlDetailedException( string message, SqlException ex )
            : base( message, ex )
        {
        }

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
