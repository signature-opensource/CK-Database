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
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Defines which assemblies and types must be discovered.
    /// </summary>
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

        static readonly XName xAssemblyRegistererConfiguration = XNamespace.None + "AssemblyRegistererConfiguration";
        static readonly XName xExplicitClass = XNamespace.None + "ExplicitClass";

        public BuildAndRegisterConfiguration( XElement e )
        {
            _assemblyRegister = new AssemblyRegistererConfiguration( e.Element( xAssemblyRegistererConfiguration ) );
            _explicitClasses = e.Elements( xExplicitClass ).Select( c => c.Value ).ToList();
        }

        /// <summary>
        /// Gets the <see cref="AssemblyRegistererConfiguration"/> that describes assemblies that must 
        /// participate (or not) to setup.
        /// </summary>
        public AssemblyRegistererConfiguration Assemblies => _assemblyRegister;

        /// <summary>
        /// List of assembly qualified type names that must be explicitely registered 
        /// regardless of <see cref="Assemblies"/>.
        /// </summary>
        public List<string> ExplicitClasses => _explicitClasses;

    }
}
