#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.SqlServer.Setup.Model\ISqlCallContext.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

namespace CK.SqlServer
{
    /// <summary>
    /// A ISqlCallContext exposes a <see cref="ISqlCommandExecutor"/>.
    /// It is actually an optional interface: what is required is that the parameter's type exposes a
    /// <see cref="Executor"/> property or a GetExecutor() method or be itself a ISqlCommandExecutor.
    /// </summary>
    public interface ISqlCallContext 
    {
        /// <summary>
        /// Gets the <see cref="ISqlCommandExecutor"/> that can be used to support calls to the database.
        /// </summary>
        ISqlCommandExecutor Executor { get; }
    }
}
