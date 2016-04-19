#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\Configuration\BuilderAppDomainConfiguration.cs) is part of CK-Database. 
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
    /// Defines options related to Application Domain configuration used during setup phasis, and which assemblies and types must be discovered.
    /// </summary>
    [Serializable]
    public class BuildAndRegisterConfiguration
    {
        readonly AssemblyRegistererConfiguration _assemblyRegister;
        readonly List<string> _explicitClasses;

        /// <summary>
        /// Initialize a new <see cref="BuildAndRegisterConfiguration"/>.
        /// </summary>
        public BuildAndRegisterConfiguration()
        {
            _assemblyRegister = new AssemblyRegistererConfiguration();
            _explicitClasses = new List<string>();
        }

        /// <summary>
        /// Gets the <see cref="AssemblyRegistererConfiguration"/> that describes assemblies that must participate (or not) to setup.
        /// </summary>
        public AssemblyRegistererConfiguration Assemblies => _assemblyRegister;

        /// <summary>
        /// List of assembly qualified type names that must be explicitely registered regardless of <see cref="Assemblies"/>.
        /// </summary>
        public List<string> ExplicitClasses => _explicitClasses;

    }
}
