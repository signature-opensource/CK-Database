#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\StObjBuildResult.cs) is part of CK-Database. 
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
    /// Encapsulates the result of the <see cref="G:StObjContextRoot.Build"/> methods.
    /// Must be <see cref="Dispose"/>d once done.
    /// </summary>
    public class StObjBuildResult : IDisposable
    {
        readonly IActivityMonitor _monitor;

        internal StObjBuildResult( bool success, string externalVersionStamp, bool assemblyAlreadyExists, IActivityMonitor buildMonitor )
        {
            Success = success;
            ExternalVersionStamp = externalVersionStamp;
            AssemblyAlreadyExists = assemblyAlreadyExists;
            _monitor = buildMonitor;
        }

        /// <summary>
        /// Gets whether the build succeeded or the assembly with the same <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/> already exists.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets whether the assembly with the same <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/> has been found.
        /// </summary>
        public bool AssemblyAlreadyExists { get; private set; }

        /// <summary>
        /// Gets the version stamp of the built or already existing assembly.
        /// When a build has been done, it is the same as the input <see cref="BuilderFinalAssemblyConfiguration.ExternalVersionStamp"/>.
        /// </summary>
        public string ExternalVersionStamp { get; private set; }

        /// <summary>
        /// Unloads any external resources.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
