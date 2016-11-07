#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\ISqlCallContext.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Data.SqlClient;

namespace CK.SqlServer
{
    /// <summary>
    /// A ISqlCallContext exposes a <see cref="ISqlCommandExecutor"/>.
    /// It is actually an optional interface: what is required is that the parameter's type exposes a
    /// <see cref="Executor"/> property or a GetExecutor() method or be itself a ISqlCommandExecutor.
    /// This interface also exposes access to <see cref="SqlConnection"/> either by connection string or 
    /// by <see cref="ISqlConnectionStringProvider"/> objects.
    /// </summary>
    public interface ISqlCallContext 
    {
        /// <summary>
        /// Gets the <see cref="ISqlCommandExecutor"/> that can be used to support calls to the database.
        /// </summary>
        ISqlCommandExecutor Executor { get; }

        /// <summary>
        /// Gets the connection to use for a given connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The SqlConnection to use that may be already opened.</returns>
        SqlConnection this[ string connectionString ] { get; }

        /// <summary>
        /// Gets the connection to use given a connection string provider.
        /// </summary>
        /// <param name="p">The provider of the connection string.</param>
        /// <returns>The SqlConnection to use that may be already opened.</returns>
        SqlConnection this[ ISqlConnectionStringProvider p ] { get; }

        /// <summary>
        /// Gets the contoller of a connection that can be use to pre open 
        /// the connection instead of relying on local open/close.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The controller for the connection.</returns>
        ISqlConnectionController GetConnectionController( string connectionString );
    }
}
