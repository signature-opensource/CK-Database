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
    /// Discovers types that support <see cref="IAmbientContract"/> and <see cref="IAmbientService"/> marker interfaces.
    /// </summary>
    public partial class AmbientTypeCollector
    {
        readonly IActivityMonitor _monitor;
        readonly IDynamicAssembly _tempAssembly;
        readonly IServiceProvider _serviceProvider;
        readonly PocoRegisterer _pocoRegisterer;
        readonly HashSet<Assembly> _assemblies;
        readonly Dictionary<Type, StObjTypeInfo> _collector;
        readonly List<StObjTypeInfo> _roots;
        readonly string _mapName;
        readonly Func<Type, bool> _typeFilter;

        /// <summary>
        /// Initializes a new <see cref="AmbientTypeCollector"/> instance.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="serviceProvider">Service provider used for attribute constructor injection.</param>
        /// <param name="tempAssembly">The temporary <see cref="IDynamicAssembly"/>.</param>
        /// <param name="mapName">Optional map name. Defaults to the empty string.</param>
        public AmbientTypeCollector(
            IActivityMonitor monitor,
            IServiceProvider serviceProvider,
            IDynamicAssembly tempAssembly,
            Func<Type,bool> typeFilter = null,
            string mapName = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( serviceProvider == null ) throw new ArgumentNullException( nameof( serviceProvider ) );
            if( tempAssembly == null ) throw new ArgumentNullException( nameof( tempAssembly ) );
            _monitor = monitor;
            _typeFilter = typeFilter ?? (type => true);
            _tempAssembly = tempAssembly;
            _serviceProvider = serviceProvider;
            _assemblies = new HashSet<Assembly>();
            _collector = new Dictionary<Type, StObjTypeInfo>();
            _roots = new List<StObjTypeInfo>();
            _serviceCollector = new Dictionary<Type, AmbientServiceClassInfo>();
            _serviceRoots = new List<AmbientServiceClassInfo>();
            _serviceInterfaces = new Dictionary<Type, AmbientServiceInterfaceInfo>();
            _pocoRegisterer = new PocoRegisterer( typeFilter: _typeFilter );
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

        bool DoRegisterClass( Type t, out StObjTypeInfo result, out AmbientServiceClassInfo serviceInfo )
        {
            Debug.Assert( t != null && t != typeof( object ) && t.IsClass );

            // Skips already processed types.
            serviceInfo = null;
            if( _collector.TryGetValue( t, out result )
                || _serviceCollector.TryGetValue( t, out serviceInfo ) )
            {
                return false;
            }

            // Registers parent types whatever they are.
            StObjTypeInfo acParent = null;
            AmbientServiceClassInfo sParent = null;
            if( t.BaseType != typeof( object ) ) DoRegisterClass( t.BaseType, out acParent, out sParent );
            Debug.Assert( (acParent == null && sParent == null) || (acParent == null) != (sParent == null) );

            // This is an Ambient contract if:
            // - its parent is an ambient contract 
            // - or it is statically an ambient contract (via IAmbientContract support or IAmbientContractDefiner on base class)
            if( acParent != null
                || typeof( IAmbientContract ).IsAssignableFrom( t )
                || typeof( IAmbientContractDefiner ).IsAssignableFrom( t.BaseType ) )
            {
                result = CreateStObjTypeInfo( t, acParent );
                Debug.Assert( result != null );
            }
            if( sParent != null || typeof( IAmbientService ).IsAssignableFrom( t ) )
            {
                if( result != null )
                {
                    _monitor.Error( $"Type {t.FullName} is both marked with {nameof( IAmbientService )} and {nameof( IAmbientContract )} (or has been configured to be an AmbiantContract)." );
                }
                else
                {
                    serviceInfo = RegisterServiceClass( t, sParent );
                    Debug.Assert( serviceInfo != null );
                }
            }
            if( result == null && serviceInfo == null )
            {
                // Marks the type as a registered one.
                _collector.Add( t, null );
            }
            return true;
        }

        StObjTypeInfo CreateStObjTypeInfo( Type t, StObjTypeInfo parent )
        {
            StObjTypeInfo result = new StObjTypeInfo( _monitor, parent, t, _serviceProvider, !_typeFilter( t ) );
            if( result == null ) throw new Exception( $"typeInfoFactory returned null for type {t.AssemblyQualifiedName}." );
            if( !result.IsExcluded )
            {
                RegisterAssembly( t );
                if( parent == null ) _roots.Add( result );
            }
            _collector.Add( t, result );
            return result;
        }

        protected void RegisterAssembly(Type t)
        {
            var a = t.GetTypeInfo().Assembly;
            if( !a.IsDynamic ) _assemblies.Add( a );
        }

        /// <summary>
        /// Obtains the result of the collection.
        /// </summary>
        /// <returns>The result object.</returns>
        public AmbientTypeCollectorResult GetResult()
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
            AmbientContractCollectorResult contracts = GetAmbientContractResult();
            AmbientServiceCollectorResult services = new AmbientServiceCollectorResult();
            return new AmbientTypeCollectorResult( _assemblies, pocoSupport, contracts, services );
        }

        AmbientContractCollectorResult GetAmbientContractResult()
        {
            MutableItem[] allSpecializations = new MutableItem[_roots.Count];
            StObjObjectEngineMap engineMap = new StObjObjectEngineMap( _mapName, allSpecializations );
            List<List<MutableItem>> concreteClasses = new List<List<MutableItem>>();
            List<IReadOnlyList<Type>> classAmbiguities = null;
            List<Type> abstractTails = new List<Type>();
            int idxSpecialization = 0;
            foreach( StObjTypeInfo newOne in _roots )
            {
                Debug.Assert( newOne.Generalization == null );
                var deepestConcretes = new List<(MutableItem, ImplementableTypeInfo)>();
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
