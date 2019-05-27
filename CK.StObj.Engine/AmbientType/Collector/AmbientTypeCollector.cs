using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;
using CK.Setup;

namespace CK.Core
{
    /// <summary>
    /// Discovers types that support <see cref="IAmbientObject"/> and <see cref="IAmbientService"/> marker interfaces.
    /// </summary>
    public partial class AmbientTypeCollector
    {
        readonly IActivityMonitor _monitor;
        readonly IDynamicAssembly _tempAssembly;
        readonly IServiceProvider _serviceProvider;
        readonly PocoRegisterer _pocoRegisterer;
        readonly HashSet<Assembly> _assemblies;
        readonly Dictionary<Type, AmbientObjectClassInfo> _collector;
        readonly List<AmbientObjectClassInfo> _roots;
        readonly string _mapName;
        readonly Func<IActivityMonitor, Type, bool> _typeFilter;

        /// <summary>
        /// Initializes a new <see cref="AmbientTypeCollector"/> instance.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="serviceProvider">Service provider used for attribute constructor injection.</param>
        /// <param name="tempAssembly">The temporary <see cref="IDynamicAssembly"/>.</param>
        /// <param name="mapName">Optional map name. Defaults to the empty string.</param>
        /// <param name="typeFilter">Optional type filter.</param>
        public AmbientTypeCollector(
            IActivityMonitor monitor,
            IServiceProvider serviceProvider,
            IDynamicAssembly tempAssembly,
            Func<IActivityMonitor,Type,bool> typeFilter = null,
            string mapName = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( serviceProvider == null ) throw new ArgumentNullException( nameof( serviceProvider ) );
            if( tempAssembly == null ) throw new ArgumentNullException( nameof( tempAssembly ) );
            _monitor = monitor;
            _typeFilter = typeFilter ?? ((m,type) => true);
            _tempAssembly = tempAssembly;
            _serviceProvider = serviceProvider;
            _assemblies = new HashSet<Assembly>();
            _collector = new Dictionary<Type, AmbientObjectClassInfo>();
            _roots = new List<AmbientObjectClassInfo>();
            _serviceCollector = new Dictionary<Type, AmbientServiceClassInfo>();
            _serviceRoots = new List<AmbientServiceClassInfo>();
            _serviceInterfaces = new Dictionary<Type, AmbientServiceInterfaceInfo>();
            _pocoRegisterer = new PocoRegisterer( typeFilter: _typeFilter );
            _ambientServiceDetector = new AmbientTypeKindDetector();
            _ambientServiceDetector.DefineAsExternalSingleton( monitor, typeof( IPocoFactory<> ) );
            _ambientServiceDetector.DefineAsExternalScoped( monitor, typeof( IActivityMonitor ) );
            _mapName = mapName ?? String.Empty;
        }

        /// <summary>
        /// Gets the number of registered types.
        /// </summary>
        public int RegisteredTypeCount => _collector.Count;

        /// <summary>
        /// Registers multiple types. Only classes and IPoco interfaces are considered.
        /// </summary>
        /// <param name="types">Set of types.</param>
        public void SafeRegister( IEnumerable<Type> types )
        {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( var t in types )
            {
                if( t != null && t != typeof( object ) ) DoRegisterClassOrPoco( t );
            }
        }

        /// <summary>
        /// Registers a type.
        /// It must be a class or a IPoco interface otherwise an argument exception is thrown.
        /// </summary>
        /// <param name="type">Class or IPoco interface.</param>
        public void RegisterClassOrPoco( Type type )
        {
            if( type == null ) throw new ArgumentNullException( nameof( type ) );
            if( type != typeof( object ) && !DoRegisterClassOrPoco( type ) )
            {
                throw new ArgumentException( $"Must be a Class or a IPoco interface: '{type.AssemblyQualifiedName}'.", nameof( type ) );
            }
        }

        bool DoRegisterClassOrPoco( Type type )
        {
            Debug.Assert( type != null && type != typeof( object ) );
            if( type.IsClass )
            {
                DoRegisterClass( type, out _, out _ );
                return true;
            }
            if( type.IsInterface && typeof( IPoco ).IsAssignableFrom( type ) )
            {
                if( _pocoRegisterer.Register( _monitor, type ) ) RegisterAssembly( type );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Registers a class.
        /// It must be a class otherwise an argument exception is thrown.
        /// </summary>
        /// <param name="c">Class to register.</param>
        /// <returns>True if it is a new class for this collector, false if it has already been registered.</returns>
        public bool RegisterClass( Type c )
        {
            if( c == null ) throw new ArgumentNullException( nameof( c ) );
            if( !c.IsClass ) throw new ArgumentException();
            return c != typeof( object ) ? DoRegisterClass( c, out _, out _ ) : false;
        }

        bool DoRegisterClass( Type t, out AmbientObjectClassInfo result, out AmbientServiceClassInfo serviceInfo )
        {
            Debug.Assert( t != null && t != typeof( object ) && t.IsClass );

            // Skips already processed types.
            // The collector contains null AmbientObjectClassInfo value for already processed types
            // that are skipped or on error.
            serviceInfo = null;
            if( _collector.TryGetValue( t, out result )
                || _serviceCollector.TryGetValue( t, out serviceInfo ) )
            {
                return false;
            }

            // Registers parent types whatever they are.
            AmbientObjectClassInfo acParent = null;
            AmbientServiceClassInfo sParent = null;
            if( t.BaseType != typeof( object ) ) DoRegisterClass( t.BaseType, out acParent, out sParent );
            Debug.Assert( (acParent == null && sParent == null) || (acParent == null) != (sParent == null) );

            AmbientTypeKind lt = _ambientServiceDetector.GetKind( t );
            var conflictMsg = lt.GetAmbientKindCombinationError();
            if( conflictMsg != null )
            {
                _monitor.Error( $"Type {t.FullName}: {conflictMsg}." );
            }
            else
            {
                if( acParent != null || (lt == AmbientTypeKind.AmbientObject) )
                {
                    result = CreateStObjTypeInfo( t, acParent );
                    Debug.Assert( result != null );
                }
                else if( sParent != null || (lt & AmbientTypeKind.IsAmbientService) != 0 )
                {
                    serviceInfo = RegisterServiceClass( t, sParent, lt );
                    Debug.Assert( serviceInfo != null );
                }
            }
            // Marks the type as a registered one.
            if( result == null && serviceInfo == null )
            {
                _collector.Add( t, null );
            }
            return true;
        }

        AmbientObjectClassInfo CreateStObjTypeInfo( Type t, AmbientObjectClassInfo parent )
        {
            AmbientObjectClassInfo result = new AmbientObjectClassInfo( _monitor, parent, t, _serviceProvider, !_typeFilter( _monitor, t ) );
            if( !result.IsExcluded )
            {
                RegisterAssembly( t );
                if( parent == null ) _roots.Add( result );
            }
            _collector.Add( t, result );
            return result;
        }

        /// <summary>
        /// Registers an assembly for which at least one type has been handled.
        /// This is required for code generation: such assemblies are dependencies.
        /// </summary>
        /// <param name="t">The registered type.</param>
        protected void RegisterAssembly( Type t )
        {
            var a = t.Assembly;
            if( !a.IsDynamic ) _assemblies.Add( a );
        }

        /// <summary>
        /// Obtains the result of the collection.
        /// This is the root of type analysis: the whole system relies on it.
        /// </summary>
        /// <returns>The result object.</returns>
        public AmbientTypeCollectorResult GetResult()
        {
            using( _monitor.OpenInfo( "Static Type analysis." ) )
            {
                IPocoSupportResult pocoSupport;
                using( _monitor.OpenInfo( "Creating Poco Types and PocoFactory." ) )
                {
                    pocoSupport = _pocoRegisterer.Finalize( _tempAssembly.StubModuleBuilder, _monitor );
                    if( pocoSupport != null )
                    {
                        _tempAssembly.Memory.Add( typeof( IPocoSupportResult ), pocoSupport );
                        _tempAssembly.SourceModules.Add( PocoSourceGenerator.CreateModule( pocoSupport ) );
                        RegisterClass( pocoSupport.FinalFactory );
                    }
                }
                AmbientContractCollectorResult contracts;
                using( _monitor.OpenInfo( "Ambient contracts handling." ) )
                {
                    contracts = GetAmbientContractResult();
                    Debug.Assert( contracts != null );
                }
                AmbientServiceCollectorResult services;
                using( _monitor.OpenInfo( "Ambient services handling." ) )
                {
                    services = GetAmbientServiceResult( contracts );
                }
                return new AmbientTypeCollectorResult( _assemblies, pocoSupport, contracts, services, _ambientServiceDetector );
            }
        }

        AmbientContractCollectorResult GetAmbientContractResult()
        {
            MutableItem[] allSpecializations = new MutableItem[_roots.Count];
            StObjObjectEngineMap engineMap = new StObjObjectEngineMap( _mapName, allSpecializations );
            List<List<MutableItem>> concreteClasses = new List<List<MutableItem>>();
            List<IReadOnlyList<Type>> classAmbiguities = null;
            List<Type> abstractTails = new List<Type>();
            var deepestConcretes = new List<(MutableItem, ImplementableTypeInfo)>();
            int idxSpecialization = 0;

            Debug.Assert( _roots.All( info => info != null && !info.IsExcluded && info.Generalization == null),
                "_roots contains only not Excluded types." );
            foreach( AmbientObjectClassInfo newOne in _roots )
            {
                deepestConcretes.Clear();
                newOne.CreateMutableItemsPath( _monitor, _serviceProvider, engineMap, null, _tempAssembly, deepestConcretes, abstractTails );
                if( deepestConcretes.Count == 1 )
                {
                    MutableItem last = deepestConcretes[0].Item1;
                    allSpecializations[idxSpecialization++] = last;
                    var path = new List<MutableItem>();
                    last.InitializeBottomUp( null, deepestConcretes[0].Item2 );
                    path.Add( last );
                    MutableItem spec = last, toInit = last;
                    while( (toInit = toInit.Generalization) != null )
                    {
                        toInit.InitializeBottomUp( spec, null );
                        path.Add( toInit );
                        spec = toInit;
                    }
                    path.Reverse();
                    concreteClasses.Add( path );
                    foreach( var m in path ) engineMap.AddClassMapping( m.Type.Type, last );
                }
                else if( deepestConcretes.Count > 1 )
                {
                    List<Type> ambiguousPath = new List<Type>() { newOne.Type };
                    ambiguousPath.AddRange( deepestConcretes.Select( m => m.Item1.Type.Type ) );

                    if( classAmbiguities == null ) classAmbiguities = new List<IReadOnlyList<Type>>();
                    classAmbiguities.Add( ambiguousPath.ToArray() );
                }
            }
            Debug.Assert( classAmbiguities != null || idxSpecialization == allSpecializations.Length );
            Dictionary<Type, List<Type>> interfaceAmbiguities = null;
            foreach( var path in concreteClasses )
            {
                MutableItem finalType = path[path.Count - 1];
                foreach( var item in path )
                {
                    foreach( Type itf in item.Type.EnsureThisAmbientInterfaces() )
                    {
                        MutableItem alreadyMapped;
                        if( (alreadyMapped = engineMap.ToLeaf( itf )) != null )
                        {
                            if( interfaceAmbiguities == null )
                            {
                                interfaceAmbiguities = new Dictionary<Type, List<Type>>();
                                interfaceAmbiguities.Add( itf, new List<Type>() { itf, alreadyMapped.Type.Type, item.Type.Type } );
                            }
                            else
                            {
                                var list = interfaceAmbiguities.GetOrSet( itf, t => new List<Type>() { itf, alreadyMapped.Type.Type } );
                                list.Add( item.Type.Type );
                            }
                        }
                        else
                        {
                            engineMap.AddInterfaceMapping( itf, item, finalType );
                        }
                    }
                }
            }
            return new AmbientContractCollectorResult(
                engineMap,
                concreteClasses,
                classAmbiguities != null
                    ? classAmbiguities
                    : (IReadOnlyList<IReadOnlyList<Type>>)Array.Empty<IReadOnlyList<Type>>(),
                interfaceAmbiguities != null
                    ? interfaceAmbiguities.Values.Select( list => list.ToArray() ).ToArray()
                    : Array.Empty<IReadOnlyList<Type>>(),
                abstractTails );
        }

    }

}
