#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AssemblyRegisterer.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Text;

namespace CK.Core
{
    /// <summary>
    /// Enable discovering assemblies and registering of types from assemblies, respecting 
    /// the ordering of dependencies between assemblies.
    /// </summary>
    public class AssemblyRegisterer
    {
        readonly IActivityMonitor	    _monitor;
        Predicate<Assembly>             _assemblyFilter;
        Predicate<Type>                 _typeFilter;
        bool                            _publicTypesOnly;
        Dictionary<Assembly,DiscoveredInfo>	_index;
        List<DiscoveredInfo>                _list;

        public class DiscoveredInfo
        {
            internal DiscoveredInfo( Assembly a )
            {
                Assembly = a;
            }

            public int Index { get; private set; }
            public Assembly Assembly { get; private set; }
            public IReadOnlyList<Type> Types { get; private set; }

            internal void Init( IEnumerable<Type> types, int index )
            {
                Index = index;
                Types = types.ToArray();
            }
        }

        /// <summary>
        /// Initializes a new <see cref="AssemblyRegisterer"/>.
        /// </summary>
        /// <param name="monitor">Logger to use. Can not be null.</param>
        public AssemblyRegisterer( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( "monitor" );
            _monitor = monitor;
            _index = new Dictionary<Assembly, DiscoveredInfo>();
            _list = new List<DiscoveredInfo>();
        }

        /// <summary>
        /// Discovers assemblies based on the <see cref="AssemblyRegistererConfiguration"/> object.
        /// </summary>
        /// <param name="config">Configuration object.</param>
        /// <remarks>
        /// The current <see cref="AssemblyFilter"/> and <see cref="TypeFilter"/> applies.
        /// </remarks>
        public void Discover( AssemblyRegistererConfiguration config )
        {
            if( config == null ) throw new ArgumentNullException( "config" );
            using( _monitor.OpenInfo().Send( "Discovering assemblies & types from configuration." ) )
            {
                Predicate<Assembly> accept = a => !config.IgnoredAssemblyNames.Contains( a.GetName().Name )
                                                    && !config.IgnoredAssemblyNamesByPrefix.Any( p => a.GetName().Name.StartsWith( p ) )
                                                    && !a.CustomAttributes.Any( attr => attr.AttributeType.FullName == "CK.Setup.ExcludeFromSetupAttribute" );
                var prevFilter = _assemblyFilter;
                if( prevFilter != null )
                {
                    _assemblyFilter = a => prevFilter( a ) && accept( a );
                }
                else
                {
                    _assemblyFilter = accept;
                }
                try
                {
                    DiscoverRecurse( config.DiscoverRecurseAssemblyNames.Select( a => Assembly.Load( new AssemblyName(a) ) ) );
                    foreach( string a in config.DiscoverAssemblyNames ) Discover( Assembly.Load(new AssemblyName(a)) );
                }
                finally
                {
                    _assemblyFilter = prevFilter;
                }
            }
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
        /// Use <see cref="Clear(Assembly)"/> for <see cref="DiscoverRecurse(Assembly)"/> to be able to 
        /// discover again an assembly.
        /// </summary>
        public IReadOnlyList<DiscoveredInfo> Assemblies => _list;

        /// <summary>
        /// Finds an existing discovered information. Returns null if not found.
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
        /// Discovers one assembly without automatically discover assemblies referenced by it.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to discover.</param>
        /// <remarks>
        /// This method does not automatically discover assemblies referenced by this one.
        /// </remarks>
        public void Discover( Assembly assembly )
        {
            Discover( false, new Assembly[] { assembly } );
        }

        /// <summary>
        /// Ensures that the given assembly has not yet been discovered, that it is accepted by the current <see cref="AssemblyFilter"/>,
        /// and that all its dependencies have been discovered before discovering it.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to discover.</param>
        /// <remarks>
        /// This method ensures that referenced assemblies are discovered before any of their referencers.
        /// </remarks>
        public void DiscoverRecurse( Assembly assembly )
        {
            Discover( true, new Assembly[] { assembly } );
        }

        /// <summary>
        /// Ensures that the given assemblies have not yet been discovered, that they are accepted by the current <see cref="AssemblyFilter"/>,
        /// and that all their dependencies have been discovered before discovering the original assemblies.
        /// </summary>
        /// <param name="assemblies">Multiple assemblies to discover.</param>
        /// <remarks>
        /// This method ensures that referenced assemblies are discovered before any of their referencers.
        /// </remarks>
        public void DiscoverRecurse( IEnumerable<Assembly> assemblies )
        {
            Discover( true, assemblies );
        }

        void Discover( bool recurse, IEnumerable<Assembly> assemblies )
        {
            if( assemblies != null && assemblies.Any() )
            {
                foreach( var a in assemblies ) DoDiscover( a, recurse );
            }
        }

        void DoDiscover( Assembly assembly, bool recurse )
        {
            if( !_index.ContainsKey( assembly ) )
            {
                var disco = new DiscoveredInfo( assembly );
                _index.Add( assembly, disco );

                using( _monitor.OpenTrace().Send( "Discovering assembly '{0}'.", assembly.FullName ) )
                {
                    try
                    {
                        if( _assemblyFilter == null || _assemblyFilter( assembly ) )
                        {
                            if( recurse )
                            {
                                List<AssemblyName> skipped = new List<AssemblyName>();
                                foreach( AssemblyName refName in assembly.GetReferencedAssemblies() )
                                {
                                    Assembly refAssembly = Assembly.Load( refName );
                                    if( refAssembly != null )
                                    {
                                        if( _assemblyFilter == null || _assemblyFilter( refAssembly ) )
                                        {
                                            DiscoverRecurse( refAssembly );
                                        }
                                        else skipped.Add( refName );
                                    }
                                    else _monitor.Warn().Send( $"Reference '{refName}' load: null assembly." );
                                }
                                if( skipped.Count > 0 )
                                {
                                    _monitor.Trace().Send( $"References '{skipped.Select( n => n.Name ).Concatenate( "', '" )}': skipped by filter." );
                                }
                            }
                            IEnumerable<Type> types = _publicTypesOnly ? assembly.GetExportedTypes() : assembly.GetTypes();
                            if( _typeFilter != null ) types = types.Where( t => _typeFilter( t ) );
                            disco.Init( types, _list.Count );
                            _list.Add( disco );
                            _monitor.CloseGroup( $"{disco.Types.Count} types discovered." );
                        }
                        else _monitor.CloseGroup( "Skipped by filter." );
                    }
                    catch( Exception ex )
                    {
                        _monitor.OpenError().Send( ex );
                    }
                }
            }
        }

    }
}

