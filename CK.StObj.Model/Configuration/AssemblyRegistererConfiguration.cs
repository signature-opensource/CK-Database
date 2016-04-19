#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Model\Configuration\AssemblyRegistererConfiguration.cs) is part of CK-Database. 
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
    /// Holds a configuration that describes which assemblies must be processed.
    /// </summary>
    [Serializable]
    public class AssemblyRegistererConfiguration
    {
        readonly HashSet<string> _ignoredAssemblies;
        readonly List<string> _whiteListNoRecurse;
        readonly List<string> _whiteList;

        readonly static string[] _defaultIgnored = new string[]
            {
                "mscorlib", "System", "System.Core", "System.Data", "System.Xml", "System.Data.DataSetExtensions", "System.Data.Xml", "System.Data.Xml.Linq",
                "CK.Core", "CK.Monitoring", "CK.Reflection", 
                "CK.Setup.Dependency", 
                "CK.StObj.Model", "CK.StObj.Runtime", "CK.StObj.Engine", 
                "CK.Setupable.Model", "CK.Setupable.Runtime", "CK.Setupable.Engine", 
                "CK.SqlServer.Core", "CK.SqlServer.Parser", 
                "CK.SqlServer.Setup.Model", "CK.SqlServer.Setup.Runtime", "CK.SqlServer.Setup.Engine", 
                "Microsoft.CSharp", "Microsoft.Practices.ServiceLocation", "Microsoft.Practices.Unity", "Microsoft.Practices.Unity.Configuration"
            };

        /// <summary>
        /// Initializes a new <see cref="AssemblyRegistererConfiguration"/>.
        /// </summary>
        public AssemblyRegistererConfiguration()
        {
            _ignoredAssemblies = new HashSet<string>( _defaultIgnored );
            _whiteListNoRecurse = new List<string>();
            _whiteList = new List<string>();
        }

        /// <summary>
        /// Gets whether this configuration has absolutely no chance to load any assembly: <see cref="AutomaticAssemblyDiscovering"/> is false
        /// and <see cref="DiscoverAssemblyNames"/> and <see cref="DiscoverRecurseAssemblyNames"/> are both empty.
        /// </summary>
        public bool IsEmptyConfiguration
        {
            get { return AutomaticAssemblyDiscovering == false && _whiteList.Count == 0 && _whiteListNoRecurse.Count == 0; }
        }

        /// <summary>
        /// Gets or sets whether current loaded assemblies and all their 
        /// dependencies (even if they are not already loaded) must be discovered.
        /// Defaults to false.
        /// </summary>
        public bool AutomaticAssemblyDiscovering { get; set; }
        
        /// <summary>
        /// Assembly names from this list are ignored wherever they come from.
        /// Contains by default some names like "System" or "CK.Core".
        /// </summary>
        public ISet<string> IgnoredAssemblyNames
        {
            get { return _ignoredAssemblies; }
        }

        /// <summary>
        /// Assembly names from this list will be explicitely loaded but their references will NOT be discovered. 
        /// Use <see cref="DiscoverRecurseAssemblyNames"/> to recursively discover referenced assemblies.
        /// </summary>
        public IList<string> DiscoverAssemblyNames
        {
            get { return _whiteListNoRecurse; }
        }

        /// <summary>
        /// Assembly names from this list will be explicitely loaded and their references will be recursively discovered. 
        /// </summary>
        public IList<string> DiscoverRecurseAssemblyNames
        {
            get { return _whiteList; }
        }

    }
}
