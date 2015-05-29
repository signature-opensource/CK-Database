#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\ISqlCallContext.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

namespace CK.SqlServer
{
    /// <summary>
    /// A ISqlCallContext is a <see cref="ISqlParameterContext"/> that exposes a <see cref="ISqlCommandExecutor"/>.
    /// </summary>
    public interface ISqlCallContext : ISqlParameterContext
    {
        /// <summary>
        /// Gets the <see cref="ISqlCommandExecutor"/> that can be used to support calls to the database.
        /// </summary>
        ISqlCommandExecutor Executor { get; }
    }
}
