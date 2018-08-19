using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;

namespace CK.Core
{
    public partial class AmbientTypeCollector
    {
        readonly Dictionary<Type, AmbientServiceClassInfo> _serviceCollector;
        readonly List<AmbientServiceClassInfo> _serviceRoots;
        readonly Dictionary<Type, AmbientServiceInterfaceInfo> _serviceInterfaces;
        int _serviceInterfaceCount;
        int _serviceRootInterfaceCount;

        AmbientServiceClassInfo RegisterServiceClass( Type t, AmbientServiceClassInfo parent )
        {
            var serviceInfo = new AmbientServiceClassInfo( _monitor, _serviceProvider, parent, t, this, !_typeFilter( _monitor, t ) );
            if( !serviceInfo.IsExcluded )
            {
                RegisterAssembly( t );
                if( serviceInfo.Generalization == null ) _serviceRoots.Add( serviceInfo );
            }
            _serviceCollector.Add( t, serviceInfo );
            return serviceInfo;
        }

        internal AmbientServiceClassInfo FindServiceClassInfo( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t.IsClass );
            _serviceCollector.TryGetValue( t, out var info );
            return info;
        }
        internal AmbientServiceInterfaceInfo FindServiceInterfaceInfo( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t.IsInterface );
            _serviceInterfaces.TryGetValue( t, out var info );
            return info;
        }

        /// <summary>
        /// Returns null if and only if the interface type is excluded.
        /// </summary>
        internal AmbientServiceInterfaceInfo RegisterServiceInterface( Type t )
        {
            Debug.Assert( typeof( IAmbientService ).IsAssignableFrom( t ) && t != typeof( IAmbientService ) && t.IsInterface );
            if( !_serviceInterfaces.TryGetValue( t, out var info ) )
            {
                if( _typeFilter( _monitor, t ) )
                {
                    info = new AmbientServiceInterfaceInfo( t, RegisterServiceInterfaces( t.GetInterfaces() ) );
                    ++_serviceInterfaceCount;
                    if( info.Interfaces.Count == 0 ) ++_serviceRootInterfaceCount;
                }
                // Adds a null value when filtered out.
                _serviceInterfaces.Add( t, info );
            }
            return info;
        }

        internal IEnumerable<AmbientServiceInterfaceInfo> RegisterServiceInterfaces( IEnumerable<Type> interfaces )
        {
            foreach( var iT in interfaces )
            {
                if( iT != typeof( IAmbientService )
                    && typeof(IAmbientService).IsAssignableFrom( iT ) )
                {
                    var r = RegisterServiceInterface( iT );
                    if( r != null ) yield return r;
                }
            }
        }

        AmbientServiceCollectorResult GetAmbientServiceResult( AmbientContractCollectorResult contracts )
        {
            List<Type> abstractTails = null;
            bool success = InitializeRootServices( contracts.EngineMap, out var classAmbiguities, ref abstractTails );
            List<AmbientServiceClassInfo> subGraphs = new List<AmbientServiceClassInfo>();
            if( success && classAmbiguities == null )
            {
                using( _monitor.OpenInfo( "Generating Service Class mappings." ) )
                {
                    int currentCount = contracts.EngineMap.ServiceSimpleMappings.Count;
                    foreach( var s in _serviceRoots )
                    {
                        s.FillFinalClassMappings( contracts.EngineMap.ServiceSimpleMappings, subGraphs );
                    }
                    Debug.Assert( contracts.EngineMap.ServiceSimpleMappings.Values.All( c => c.MostSpecialized != null ) );
                    Debug.Assert( subGraphs.All( c => c.MostSpecialized != null ) );
                    int deltaMappings = contracts.EngineMap.ServiceSimpleMappings.Count - currentCount;
                    _monitor.CloseGroup( $"{deltaMappings} mappings for {_serviceRoots.Count} root services with {subGraphs.Count} sub graphs." );
                }
            }
            // Collecting all, roots and leaves interfaces.
            var leafInterfaces = new List<AmbientServiceInterfaceInfo>();
            var allInterfaces = new AmbientServiceInterfaceInfo[_serviceInterfaceCount];
            var rootInterfaces = new AmbientServiceInterfaceInfo[_serviceRootInterfaceCount];
            int idxAll = 0;
            int idxRoot = 0;
            foreach( var it in _serviceInterfaces.Values )
            {
                if( it != null )
                {
                    allInterfaces[idxAll++] = it;
                    if( !it.IsSpecialized ) leafInterfaces.Add( it );
                    if( it.Interfaces.Count == 0 ) rootInterfaces[idxRoot++] = it;
                }
            }
            Debug.Assert( idxAll == allInterfaces.Length );
            Debug.Assert( idxRoot == rootInterfaces.Length );
            _monitor.Info( $"{allInterfaces.Length} Service interfaces with {rootInterfaces.Length} roots and {leafInterfaces.Count} interface leaves." );
            return new AmbientServiceCollectorResult(
                success,
                allInterfaces,
                leafInterfaces,
                rootInterfaces,
                _serviceRoots,
                classAmbiguities,
                abstractTails,
                subGraphs );
        }

        bool InitializeRootServices(
            StObjObjectEngineMap engineMap,
            out IReadOnlyList<IReadOnlyList<AmbientServiceClassInfo>> classAmbiguities,
            ref List<Type> abstractTails )
        {
            using( _monitor.OpenInfo( $"Analysing {_serviceRoots.Count} Service class hierarchies." ) )
            {
                bool error = false;
                var deepestConcretes = new List<AmbientServiceClassInfo>();
                Debug.Assert( _serviceRoots.All( info => info != null && !info.IsExcluded && info.Generalization == null ),
                    "_serviceRoots contains only not Excluded types." );
                List<(AmbientServiceClassInfo Root, AmbientServiceClassInfo[] Leaves)> ambiguities = null;
                // We must wait until all paths have been initialized before ensuring constructor parameters
                AmbientServiceClassInfo[] resolvedLeaves = new AmbientServiceClassInfo[_serviceRoots.Count];
                for( int i = 0; i < _serviceRoots.Count; ++i )
                {
                    var c = _serviceRoots[i];
                    deepestConcretes.Clear();
                    if( !c.InitializePath( _monitor, this, null, _tempAssembly, deepestConcretes, ref abstractTails ) )
                    {
                        _monitor.Warn( $"Service '{c.Type.Name}' is abstract. It is ignored." );
                        _serviceRoots.RemoveAt( i-- );
                        continue;
                    }
                    // If deepestConcretes is empty it means that the whole chain is purely abstract.
                    // We ignore it.
                    if( deepestConcretes.Count == 1 )
                    {
                        // No specialization ambiguities: no class unification required.
                        resolvedLeaves[i] = deepestConcretes[0];
                    }
                    else if( deepestConcretes.Count > 1 )
                    {
                        if( ambiguities == null ) ambiguities = new List<(AmbientServiceClassInfo, AmbientServiceClassInfo[])>();
                        ambiguities.Add( (c, deepestConcretes.ToArray()) );
                    }
                }
                _monitor.Trace( $"Found {_serviceRoots.Count} unambiguous paths." );
                // Initializes all non ambiguous paths.
                for( int i = 0; i < _serviceRoots.Count; ++i )
                {
                    var leaf = resolvedLeaves[i];
                    if( leaf != null )
                    {
                        error |= !leaf.EnsureCtorBinding( _monitor, this )
                                 || !_serviceRoots[i].SetMostSpecialized( _monitor, engineMap, leaf );
                    }
                }
                // Every non ambiguous paths have been initialized.
                // Now, if there is no initialization error, tries to resolve class ambiguities.
                List<IReadOnlyList<AmbientServiceClassInfo>> remainingAmbiguities = null;
                if( !error && ambiguities != null )
                {
                    using( _monitor.OpenInfo( $"Trying to resolve {ambiguities.Count} ambiguities." ) )
                    {
                        var resolver = new ClassAmbiguityResolver( _monitor, this, engineMap );
                        foreach( var a in ambiguities )
                        {
                            using( _monitor.OpenTrace( $"Trying to resolve class ambiguities for {a.Root.Type.Name}." ) )
                            {
                                var (success, initError) = resolver.TryClassUnification( a.Root, a.Leaves );
                                error |= initError;
                                if( success )
                                {
                                    _monitor.CloseGroup( "Succeeds, resolved to: " + a.Root.MostSpecialized.Type.Name );
                                }
                                else
                                {
                                    _monitor.CloseGroup( "Failed." );
                                    if( remainingAmbiguities == null ) remainingAmbiguities = new List<IReadOnlyList<AmbientServiceClassInfo>>();
                                    resolver.CollectRemainingAmbiguities( remainingAmbiguities );
                                }
                            }
                        }
                    }
                }
                classAmbiguities = remainingAmbiguities;
                return !error;
            }
        }

        class ClassAmbiguityResolver
        {
            readonly Dictionary<AmbientServiceClassInfo, ClassAmbiguity> _ambiguities;
            readonly IActivityMonitor _monitor;
            readonly AmbientTypeCollector _collector;
            readonly StObjObjectEngineMap _engineMap;

            AmbientServiceClassInfo _root;
            AmbientServiceClassInfo _rootAmbiguity;
            AmbientServiceClassInfo[] _allLeaves;

            struct ClassAmbiguity
            {
                public readonly AmbientServiceClassInfo Class;
                public readonly List<AmbientServiceClassInfo> Leaves;

                public ClassAmbiguity( AmbientServiceClassInfo c )
                {
                    Debug.Assert( c.SpecializationsCount > 0 && c.MostSpecialized == null );
                    Class = c;
                    Leaves = new List<AmbientServiceClassInfo>();
                }
            }

            public ClassAmbiguityResolver( IActivityMonitor monitor, AmbientTypeCollector collector, StObjObjectEngineMap engineMap )
            {
                _ambiguities = new Dictionary<AmbientServiceClassInfo, ClassAmbiguity>();
                _monitor = monitor;
                _collector = collector;
                _engineMap = engineMap;
            }

            public (bool Success, bool InitializationError) TryClassUnification( AmbientServiceClassInfo root, AmbientServiceClassInfo[] allLeaves  )
            {
                Debug.Assert( allLeaves.Length > 1
                              && NextUpperAmbiguity( allLeaves[0] ) != null
                              && NextUpperAmbiguity( allLeaves[1] ) != null );
                _root = root;
                _allLeaves = allLeaves;
                while( root.SpecializationsCount == 1 )
                {
                    root = root.Specializations.Single();
                }
                _rootAmbiguity = root;
                bool allSuccess = true;
                bool initializationError = !Initialize();
                foreach( var ca in _ambiguities.Values.OrderByDescending( a => a.Class.SpecializationDepth ) )
                {
                    var (success, initError) = Resolve( ca );
                    initializationError |= initError;
                    allSuccess &= success;
                }
                if( allSuccess
                    && !initializationError
                    && _root != _rootAmbiguity )
                {
                    Debug.Assert( _root.MostSpecialized == null );
                    Debug.Assert( _rootAmbiguity.MostSpecialized != null );
                    initializationError |= !_root.SetMostSpecialized( _monitor, _engineMap, _rootAmbiguity.MostSpecialized );
                }
                return (allSuccess,initializationError);
            }

            public void CollectRemainingAmbiguities( List<IReadOnlyList<AmbientServiceClassInfo>> ambiguities )
            {
                Debug.Assert( _ambiguities.Count > 0 );
                foreach( var ca in _ambiguities.Values )
                {
                    if( ca.Class.MostSpecialized == null )
                    {
                        ca.Leaves.Insert( 0, ca.Class );
                        ambiguities.Add( ca.Leaves );
                    }
                }
                _ambiguities.Clear();
            }

            bool Initialize()
            {
                _ambiguities.Clear();
                bool initializationError = false;
                foreach( var leaf in _allLeaves )
                {
                    leaf.GetCtorParametersClassClosure( _monitor, _collector, ref initializationError );
                    var a = NextUpperAmbiguity( leaf );
                    while( a != null )
                    {
                        if( !_ambiguities.TryGetValue( a, out ClassAmbiguity ca ) )
                        {
                            ca = new ClassAmbiguity( a );
                            _ambiguities.Add( a, ca );
                        }
                        ca.Leaves.Add( leaf );
                        a = NextUpperAmbiguity( a );
                    }
                }
                return !initializationError;
            }

            (bool Success, bool InitializationError) Resolve( ClassAmbiguity ca )
            {
                bool success = false;
                bool initializationError = false;
#if DEBUG
                // Count is used to assert the fact that not 2 leaves should match.
                int resolvedPathCount = 0;
#endif
                var a = ca.Class;
                foreach( var leaf in ca.Leaves )
                {
                    bool thisPathIsResolved = true;
                    var closure = leaf.ComputedCtorParametersClassClosure;
                    bool isLeafUnifier = a.Specializations
                                            .Where( s => !s.IsAssignableFrom( leaf ) )
                                            .All( s => closure.Contains( s ) );
                    if( isLeafUnifier )
                    {
                        if( a.MostSpecialized != null )
                        {
                            _monitor.Error( $"Class Unification ambiguity: '{a.Type.Name}' is already resolved by '{a.MostSpecialized.Type.FullName}'. It can not be resolved also by '{leaf.Type.FullName}'." );
                            thisPathIsResolved = false;
                        }
                        else
                        {
                            _monitor.Trace( $"Class Unification: '{a.Type.Name}' resolved to '{leaf.Type.FullName}'." );
                            initializationError |= !a.SetMostSpecialized( _monitor, _engineMap, leaf );
                        }
                    }
                    else
                    {
                        thisPathIsResolved = false;
                    }
                    // If this leaf worked, it must be the very first one: subsequent ones must fail.
                    Debug.Assert( !thisPathIsResolved || ++resolvedPathCount == 1 );
                    success |= thisPathIsResolved;
                }
                if( !success )
                {
                    _monitor.Error( $"Service Class Unification: unable to resolve '{a.Type.Name}' to a unique specialization." );
                }
                return (success, initializationError);
            }

            static AmbientServiceClassInfo NextUpperAmbiguity( AmbientServiceClassInfo start )
            {
                var g = start.Generalization;
                while( g != null )
                {
                    if( g.SpecializationsCount > 1 ) break;
                    g = g.Generalization;
                }
                return g;
            }
        }
    }


}
