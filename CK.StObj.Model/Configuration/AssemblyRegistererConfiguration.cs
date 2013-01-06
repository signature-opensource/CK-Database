using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Holds a configuration that describes which assemblies must be processed.
    /// </summary>
    public class AssemblyRegistererConfiguration
    {
        readonly HashSet<string> _ignoredAssemblies;
        readonly List<string> _whiteListNoRecurse;
        readonly List<string> _whiteList;

        readonly static string[] _defaultIgnored = new string[]
            {
                "System", "System.Core", "System.Data", "System.Data.DataSetExtensions", "System.Data.Xml", "System.Data.Xml.Linq",
                "CK.Core", "CK.Setup.Dependency", "CK.Setup.StObj", "CK.Setup.Database", "CK.SqlServer", "CK.Setup",
                "Microsoft.CSharp", "Microsoft.Practices.ServiceLocation", "Microsoft.Practices.Unity", "Microsoft.Practices.Unity.Configuration"
            };

        public AssemblyRegistererConfiguration()
        {
            _ignoredAssemblies = new HashSet<string>( _defaultIgnored );
            _whiteListNoRecurse = new List<string>();
            _whiteList = new List<string>();
        }

        /// <summary>
        /// Gets or sets whether assemblies loaded in the <see cref="AppDomain.CurrentDomain"/> and all their 
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
