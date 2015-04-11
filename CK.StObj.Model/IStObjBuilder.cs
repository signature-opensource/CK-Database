#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\IStObjBuilder.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines an entry point that triggers the build of the system.
    /// This interface should be supported by an object with a public constructor that accepts
    /// a <see cref="IActivityMonitor"/> and a <see cref="IStObjBuilderConfiguration"/> (its assembly qualified name 
    /// must be specified as the <see cref="IStObjBuilderConfiguration.BuilderAssemblyQualifiedName"/> property).
    /// </summary>
    public interface IStObjBuilder
    {
        /// <summary>
        /// Runs the full build of the system.
        /// </summary>
        /// <returns>True on success, null if an error occurred.</returns>
        bool Run();
    }
}
