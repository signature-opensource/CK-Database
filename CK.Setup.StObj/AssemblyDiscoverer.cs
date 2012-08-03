using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Enable discovering of types from assemblies, respecting the ordering of dependencies between them.
    /// </summary>
    public class AssemblyDiscoverer
    {
        readonly IActivityLogger	    _logger;
        Predicate<Assembly>             _assemblyFilter;
        Predicate<Type>                 _typeFilter;
        bool                            _publicTypesOnly;
        AssemblyLoadEventHandler	    _onLoad;
        Dictionary<Assembly,DiscoveredInfo>	_index;
        List<DiscoveredInfo>                _list;
        IReadOnlyList<DiscoveredInfo>       _listEx;

        public class DiscoveredInfo
        {
            internal DiscoveredInfo( Assembly a )
            {
                Assembly = a;
            }

            public int Index { get; private set; }
            public Assembly Assembly { get; private set; }
            public IReadOnlyList<Type> Types { get; private set; }

            internal void Init( IReadOnlyList<Type> types, int index )
            {
                Index = index;
                Types = types;
            }
        }

        /// <summary>
        /// Initializes a new <see cref="AssemblyDiscoverer"/>.
        /// </summary>
        /// <param name="logger">Logger to use. Can not be null.</param>
        public AssemblyDiscoverer( IActivityLogger logger )
        {
            if( _logger == null ) throw new ArgumentNullException( "logger" );
            _logger = logger;
            _index = new Dictionary<Assembly, DiscoveredInfo>();
            _list = new List<DiscoveredInfo>();
            _listEx = new ReadOnlyListOnIList<DiscoveredInfo>( _list );
        }

        /// <summary>
        /// Gets or sets a filter for processed assemblies: if this filter returns false for an assembly, it is skipped and 
        /// its dependencies are not processed.
        /// Can be null: all assemblies are processed.
        /// </summary>
        public Predicate<Assembly> AssemblyFilter 
        { 
            get { return _assemblyFilter; } 
            set { _assemblyFilter = value; } 
        }

        /// <summary>
        /// Gets or sets a filter for discovered types: if this filter returns false for a type, it will not be 
        /// kept in the <see cref="DiscoveredInfo.Types"/> list.
        /// Can be null: all types are kept.
        /// </summary>
        public Predicate<Type> TypeFilter
        {
            get { return _typeFilter; }
            set { _typeFilter = value; }
        }

        /// <summary>
        /// Gets or set whether only types visible outside the assemblies should be kept in <see cref="DiscoveredInfo.Types"/>.
        /// Defauls to false: internal types are dicovered.
        /// </summary>
        public bool KeepPublicTypesOnly
        {
            get { return _publicTypesOnly; }
            set { _publicTypesOnly = value; }
        }
        
        /// <summary>
        /// Gets the list of assemblies that have been discovered so far. 
        /// Use <see cref="ClearDiscoveredInfo"/> for <see cref="Discover"/> to be able to 
        /// discover again an assembly.
        /// </summary>
        public IReadOnlyList<DiscoveredInfo> AssembliesProcessed
        {
            get { return _listEx; }
        }

        /// <summary>
        /// Finds an existing <see cref="Discover"/>ed information. Returns null if not found.
        /// </summary>
        /// <param name="a">The assembly.</param>
        /// <returns>A <see cref="DiscoveredInfo"/> or null if not found.</returns>
        public DiscoveredInfo Find( Assembly a )
        {
            return _index.GetValueWithDefault( a, null );
        }

        /// <summary>
        /// Clears discovered information for an assembly.
        /// </summary>
        public bool Clear( Assembly a )
        {
            if( a == null ) throw new ArgumentNullException( "a" );
            DiscoveredInfo d;
            if( _index.TryGetValue( a, out d ) )
            {
                _index.Remove( a );
                _list.RemoveAt( d.Index );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all discovered information.
        /// </summary>
        public void Clear()
        {
            _index.Clear();
            _list.Clear();
        }

        /// <summary>
        /// Discover assemblies currently loaded in the <see cref="AppDomain.CurrentDomain"/>.
        /// </summary>
        public void DiscoverCurrenlyLoadedAssemblies()
        {
            Discover( AppDomain.CurrentDomain.GetAssemblies() );
        }

        /// <summary>
        /// Ensures that the given assemblies has not yet been discovered, that it is accepted by the current <see cref="AssemblyFilter"/>,
        /// and that all its dependencies have been discovered before discovering it.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to discover.</param>
        /// <remarks>
        /// This method ensures that referenced assemblies are discovered before any of their referencers.
        /// </remarks>
        public void Discover( Assembly assembly )
        {
            Discover( new Assembly[] { assembly } );
        }

        /// <summary>
        /// Ensures that the given assemblies have not yet been discovered, that they are accepted by the current <see cref="AssemblyFilter"/>,
        /// and that all their dependencies have been discovered before discovering the original assemblies.
        /// </summary>
        /// <param name="assembly">Multiple assemblies to discover.</param>
        /// <remarks>
        /// This method ensures that referenced assemblies are discovered before any of their referencers.
        /// </remarks>
        public void Discover( params Assembly[] assembly )
        {
            if( assembly != null && assembly.Length > 0 )
            {
                var onLoad = new AssemblyLoadEventHandler( AssemblyLoadHandler );
                AppDomain.CurrentDomain.AssemblyLoad += onLoad;
                try
                {
                    foreach( var a in assembly ) DoDiscover( a );
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyLoad -= onLoad;
                }
            }
        }

        void DoDiscover( Assembly assembly )
        {
            if( !_index.ContainsKey( assembly ) )
            {
                var disco = new DiscoveredInfo( assembly );
                _index.Add( assembly, disco );

                using( _logger.OpenGroup( LogLevel.Info, "Discovering assembly '{0}'.", assembly.FullName ) )
                {
                    try
                    {
                        if( _assemblyFilter == null || _assemblyFilter( assembly ) )
                        {
                            foreach( AssemblyName refName in assembly.GetReferencedAssemblies() )
                            {
                                Assembly refAssembly = Assembly.Load( refName );
                                if( refAssembly != null ) Discover( refAssembly );
                            }
                            IEnumerable<Type> types = _publicTypesOnly ? assembly.GetExportedTypes() : assembly.GetTypes();
                            if( _typeFilter != null ) types = types.Where( t => _typeFilter( t ) );
                            disco.Init( types.ToReadOnlyList(), _list.Count );
                            _list.Add( disco );
                            _logger.CloseGroup( String.Format( "{0} types discovered.", disco.Types.Count ) );
                        }
                        else _logger.CloseGroup( "Skipped by filter." );
                    }
                    catch( Exception ex )
                    {
                        _logger.Error( ex );
                    }
                }
            }
        }

        void AssemblyLoadHandler( object o, AssemblyLoadEventArgs e )
        {
            DoDiscover( e.LoadedAssembly );
        }

    }
}

