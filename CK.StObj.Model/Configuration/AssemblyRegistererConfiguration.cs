using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CK.Core
{
    /// <summary>
    /// Holds a configuration that describes which assemblies must be processed.
    /// </summary>
    public class AssemblyRegistererConfiguration
    {
        readonly HashSet<string> _ignoredAssemblies;
        readonly HashSet<string> _ignoredAssembliesByPrefix;
        readonly List<string> _discoverAssemblyNames;
        readonly List<string> _discoverRecurseAssemblyNames;

        readonly static string[] _defaultIgnored = new string[]
            {
                "mscorlib", "System",
                "CK.Core", "CK.Text", "CK.Reflection",
                "CK.Setup.Dependency",
                "Newtonsoft.Json", "Dapper"
            };

        readonly static string[] _defaultPrefixIgnored = new string[]
            {
                "CK.ActivityMonitor",
                "CK.StObj.",
                "CK.Setupable.",
                "CK.SqlServer.",
                "Microsoft.",
                "System."
            };

        /// <summary>
        /// Initializes a new <see cref="AssemblyRegistererConfiguration"/>.
        /// </summary>
        public AssemblyRegistererConfiguration()
        {
            _ignoredAssemblies = new HashSet<string>( _defaultIgnored, StringComparer.OrdinalIgnoreCase );
            _ignoredAssembliesByPrefix = new HashSet<string>( _defaultPrefixIgnored, StringComparer.OrdinalIgnoreCase );
            _discoverAssemblyNames = new List<string>();
            _discoverRecurseAssemblyNames = new List<string>();
        }

        static readonly XName xIgnoredAssemby = XNamespace.None + "IgnoredAssembly";
        static readonly XName xIgnoredAssembyByPrefix = XNamespace.None + "IgnoredAssembyByPrefix";
        static readonly XName xDiscoverAssemblyName = XNamespace.None + "DiscoverAssemblyName";
        static readonly XName xDiscoverRecurseAssemblyName = XNamespace.None + "DiscoverRecurseAssemblyName";

        /// <summary>
        /// Initializes a new <see cref="AssemblyRegistererConfiguration"/> from a <see cref="XElement"/>.
        /// </summary>
        /// <param name="e">The xml element.</param>
        public AssemblyRegistererConfiguration( XElement e )
        {
            _ignoredAssemblies = new HashSet<string>( _defaultIgnored, StringComparer.OrdinalIgnoreCase );
            _ignoredAssembliesByPrefix = new HashSet<string>( _defaultPrefixIgnored, StringComparer.OrdinalIgnoreCase );
            _ignoredAssemblies.AddRange( e.Elements( xIgnoredAssemby ).Select( i => i.Value ) );
            _ignoredAssembliesByPrefix.AddRange( e.Elements( xIgnoredAssembyByPrefix ).Select( i => i.Value ) );
            _discoverAssemblyNames = e.Elements( xDiscoverAssemblyName ).Select( i => i.Value ).ToList();
            _discoverRecurseAssemblyNames = e.Elements( xDiscoverRecurseAssemblyName ).Select( i => i.Value ).ToList();
        }

        /// <summary>
        /// Serializes its content in the provided <see cref="XElement"/> and returns it.
        /// The <see cref="AssemblyRegistererConfiguration(XElement)"/> constructor will be able to read this element back.
        /// </summary>
        /// <param name="e">The element to populate.</param>
        /// <returns>The <paramref name="e"/> element.</returns>
        public XElement SerializeXml( XElement e )
        {
            e.Add( _ignoredAssemblies.Select( n => new XElement( xIgnoredAssemby, n ) ),
                   _ignoredAssembliesByPrefix.Select( n => new XElement( xIgnoredAssembyByPrefix, n ) ),
                   _discoverAssemblyNames.Select( n => new XElement( xDiscoverAssemblyName, n ) ),
                   _discoverRecurseAssemblyNames.Select( n => new XElement( xDiscoverRecurseAssemblyName, n ) ));
            return e;
        }

        /// <summary>
        /// Gets whether this configuration has absolutely no chance to load any assembly: <see cref="DiscoverAssemblyNames"/> and <see cref="DiscoverRecurseAssemblyNames"/> are both empty.
        /// </summary>
        public bool IsEmptyConfiguration => _discoverRecurseAssemblyNames.Count == 0 && _discoverAssemblyNames.Count == 0;

        /// <summary>
        /// Assembly names from this list are ignored wherever they come from.
        /// Contains by default some names like "CK.Core" or "Newtonsoft.Json".
        /// Match is case insensitive.
        /// </summary>
        public ISet<string> IgnoredAssemblyNames => _ignoredAssemblies; 

        /// <summary>
        /// Assembly names prefix from this list are ignored wherever they come from.
        /// Contains by default names like "System.", "Microsoft.", "CK.SqlServer.".
        /// Match is case insensitive.
        /// </summary>
        public ISet<string> IgnoredAssemblyNamesByPrefix => _ignoredAssembliesByPrefix; 

        /// <summary>
        /// Assembly names from this list will be explicitely loaded but their references will NOT be discovered. 
        /// Use <see cref="DiscoverRecurseAssemblyNames"/> to recursively discover referenced assemblies.
        /// </summary>
        public IList<string> DiscoverAssemblyNames => _discoverAssemblyNames; 

        /// <summary>
        /// Assembly names from this list will be explicitely loaded and their references will be recursively discovered. 
        /// </summary>
        public IList<string> DiscoverRecurseAssemblyNames => _discoverRecurseAssemblyNames; 

    }
}
