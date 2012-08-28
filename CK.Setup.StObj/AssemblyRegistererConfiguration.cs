using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Holds a static configuration that can be used by <see cref="AssemblyRegisterer"/>.
    /// </summary>
    public class AssemblyRegistererConfiguration
    {
        HashSet<string> _ignoredAssemblies;
        List<string> _whiteListNoRecurse;
        List<string> _whiteList;

        string[] _defaultIgnored = new string[]
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
        /// <remarks>
        /// This makes little sense to use this list in conjunction with <see cref="AutomaticAssemblyDiscovering"/> even if assemblies 
        /// in this list will be loaded after the recursive of currently loaded assemblies.
        /// </remarks>
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
