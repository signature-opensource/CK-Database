using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CK.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Discovers types that support <see cref="IAmbientContract"/> and <see cref="IAmbientService"/> marker interfaces
    /// and manages to dispatch them among different contexts (identified by a string) with generalization/specialization
    /// handling.
    /// </summary>
    /// <remarks>
    /// The default context is identified by the empty string and contains all <see cref="IAmbientContract"/> and
    /// <see cref="IAmbientService"/> that are not explicitely associated to a specific context.
    /// </remarks>
    public class AmbientContractCollector
    {
        /// <summary>
        /// Simple helper that centralizes the formatting of a context associated to a type.
        /// </summary>
        /// <param name="context">Context. Can be null or empty.</param>
        /// <param name="type">Type for which a contextualized name must be obtained.</param>
        /// <returns>Contextual name of the type.</returns>
        /// <remarks>
        /// Choosen format [Context]TypeFullName mimics the way objects are addressed in CK.Setup only
        /// for homogeneity. Unique naming of contextualized types (used by the dependency sorter to resolve dependency order) has, strictly
        /// speaking, nothing to do with setup full names. Nevertheless, it seems a good idea to rely on the same (simple) format.
        /// </remarks>
        static public string FormatContextualFullName( string context, Type type )
        {
            if( type == null ) throw new ArgumentNullException( "type" );
            return context == null ? type.FullName : '[' + context + ']' + type.FullName;
        }

        protected void RegisterAssembly(Type t)
        {
            var a = t.GetTypeInfo().Assembly;
            if( !a.IsDynamic ) Assemblies.Add( a );
        }

        /// <summary>
        /// The set of assemblies for which at least one type has been registered.
        /// </summary>
        protected HashSet<Assembly> Assemblies = new HashSet<Assembly>();
    }

    /// <summary>
    /// Typed implementation of an <see cref="AmbientContractCollector"/>.
    /// </summary>
    /// <typeparam name="CT">A <see cref="AmbientContextualTypeMap{T,TC}"/> type.</typeparam>
    /// <typeparam name="T">A <see cref="AmbientTypeInfo"/> type.</typeparam>
    /// <typeparam name="TC">A <see cref="AmbientContextualTypeInfo{T,TC}"/> type.</typeparam>
    public partial class AmbientContractCollector<CT,T,TC> : AmbientContractCollector
        where CT : AmbientContextualTypeMap<T, TC>
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T, TC>
    {
        readonly Dictionary<Type, T> _collector;
        readonly List<T> _roots;

        readonly IAmbientContractDispatcher _contextDispatcher;

        readonly Func<IActivityMonitor, AmbientTypeMap<CT>> _mapFactory;
        readonly Func<IActivityMonitor,T,Type,T> _typeInfoFactory;
        
        readonly IActivityMonitor _monitor;
        readonly IDynamicAssembly _tempAssembly;
        readonly PocoRegisterer _pocoRegisterer;

        /// <summary>
        /// Initializes a new <see cref="AmbientContractCollector{CT,T,TC}"/> instance.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="mapFactory">Factory for <see cref="IContextualTypeMap"/> objects.</param>
        /// <param name="typeInfoFactory">Factory for <see cref="AmbientTypeInfo"/> objects.</param>
        /// <param name="tempAssembly">The temporary <see cref="IDynamicAssembly"/>.</param>
        /// <param name="contextDispatcher">The strategy that will be used to alter type dispatching.</param>
        public AmbientContractCollector( 
            IActivityMonitor monitor,
            Func<IActivityMonitor, AmbientTypeMap<CT>> mapFactory,
            Func<IActivityMonitor, T, Type, T> typeInfoFactory,
            IDynamicAssembly tempAssembly,
            IAmbientContractDispatcher contextDispatcher = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( mapFactory == null ) throw new ArgumentNullException( nameof( mapFactory ) );
            if( typeInfoFactory == null ) throw new ArgumentNullException( nameof( typeInfoFactory ) );
            if( tempAssembly == null ) throw new ArgumentNullException( nameof( tempAssembly ) );
            _monitor = monitor;
            _contextDispatcher = contextDispatcher;
            _tempAssembly = tempAssembly;
            _collector = new Dictionary<Type, T>();
            _roots = new List<T>();
            _serviceCollector = new Dictionary<Type, AmbientServiceClassInfo>();
            _serviceRoots = new List<AmbientServiceClassInfo>();
            _serviceInterfaces = new Dictionary<Type, AmbientServiceInterfaceInfo>();
            _mapFactory = mapFactory;
            _typeInfoFactory = typeInfoFactory;
            _pocoRegisterer = new PocoRegisterer();
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
                if( t != null && t != typeof(object) ) DoRegisterClassOrPoco( t );
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
                RegisterAssembly( type );
                _pocoRegisterer.Register( _monitor, type );
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
            return c != typeof(object) ? DoRegisterClass( c, out _, out _ ) : false;
        }

        bool DoRegisterClass( Type t, out T result, out AmbientServiceClassInfo serviceInfo )
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
            T acParent = null;
            AmbientServiceClassInfo sParent = null;
            if( t.BaseType != typeof( object ) ) DoRegisterClass( t.BaseType, out acParent, out sParent );
            Debug.Assert( (acParent == null && sParent == null) || (acParent == null) != (sParent == null) );

            // This is an Ambient contract if:
            // - its parent is an ambient contract 
            // - or it is statically an ambient contract (via IAmbientContract support or IAmbientContractDefiner on base class)
            // - or the IAmbientContractDispatcher wants to consider it as one.
            if( acParent != null
                || typeof( IAmbientContract ).IsAssignableFrom( t )
                || typeof( IAmbientContractDefiner ).IsAssignableFrom( t.BaseType )
                || (_contextDispatcher != null && _contextDispatcher.IsAmbientContractClass( t )) )
            {
                result = CreateTypeInfo( t, acParent );
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
                    if( _contextDispatcher != null ) _contextDispatcher.Dispatch( t, serviceInfo.MutableFinalContexts );
                }
            }
            if( result == null && serviceInfo == null )
            {
                // Marks the type as a registered one.
                _collector.Add( t, null );
            }
            return true;
        }

        T CreateTypeInfo( Type t, T parent )
        {
            RegisterAssembly( t );
            T result = _typeInfoFactory( _monitor, parent, t );
            if( result == null ) throw new Exception( $"typeInfoFactory returned null for type {t.AssemblyQualifiedName}." );
            if( parent == null ) _roots.Add( result );
            _collector.Add( t, result );
            if( _contextDispatcher != null ) _contextDispatcher.Dispatch( t, result.MutableFinalContexts );
            return result;
        }

        class PreResult
        {
            public readonly CT Context;
            readonly IActivityMonitor _monitor;
            readonly IServiceProvider _serviceProvider;
            readonly IDynamicAssembly _tempAssembly;

            Dictionary<object,TC> _mappings;
            List<List<TC>> _concreteClasses;
            List<IReadOnlyList<Type>> _classAmbiguities;
            List<Type> _abstractTails;
            int _registeredCount;

            public PreResult( IActivityMonitor monitor, CT c, IServiceProvider services, IDynamicAssembly tempAssembly )
            {
                Debug.Assert( c != null );
                Context = c;
                _monitor = monitor;
                _serviceProvider = services;
                _tempAssembly = tempAssembly;
                _mappings = c.RawMappings;
                _concreteClasses = new List<List<TC>>();
                _abstractTails = new List<Type>();
            }

            public void Add( AmbientTypeInfo newOne )
            {
                ++_registeredCount;
                if( newOne.Generalization == null )
                {
                    var deepestConcretes = new List<Tuple<TC,object>>();
                    newOne.CollectDeepestConcrete<T, TC>( _monitor, _serviceProvider, Context, null, _tempAssembly, deepestConcretes, _abstractTails );
                    if( deepestConcretes.Count == 1 )
                    {
                        var last = deepestConcretes[0].Item1;
                        var path = new List<TC>();
                        last.InitializeBottomUp( null, deepestConcretes[0].Item2 );
                        path.Add( last );
                        TC spec = last, toInit = last;
                        while( (toInit = toInit.Generalization) != null )
                        {
                            toInit.InitializeBottomUp( spec, null );
                            path.Add( toInit );
                            spec = toInit;
                        }
                        path.Reverse();
                        _concreteClasses.Add( path );
                        foreach( var m in path ) _mappings.Add( m.AmbientTypeInfo.Type, last );                    
                    }
                    else if( deepestConcretes.Count > 1 )
                    {
                        List<Type> ambiguousPath = new List<Type>() { newOne.Type };
                        ambiguousPath.AddRange( deepestConcretes.Select( m => m.Item1.AmbientTypeInfo.Type ) );

                        if( _classAmbiguities == null ) _classAmbiguities = new List<IReadOnlyList<Type>>();
                        _classAmbiguities.Add( ambiguousPath.ToArray() );
                    }
                }
            }

            public AmbientContractCollectorContextualResult<CT,T,TC> GetResult( AmbientTypeMap<CT> allMappings, Func<Type, bool> ambientInterfacePredicate )
            {
                Dictionary<Type,List<Type>> interfaceAmbiguities = null;
                foreach( List<TC> path in _concreteClasses )
                {
                    var finalType = path[path.Count - 1];
                    foreach( TC ctxType in path )
                    {
                        foreach( Type itf in ctxType.AmbientTypeInfo.EnsureThisAmbientInterfaces( ambientInterfacePredicate ) )
                        {
                            TC alreadyMapped;
                            if( _mappings.TryGetValue( itf, out alreadyMapped ) )
                            {
                                if( interfaceAmbiguities == null ) 
                                {
                                    interfaceAmbiguities = new Dictionary<Type,List<Type>>();
                                    interfaceAmbiguities.Add( itf, new List<Type>() { itf, alreadyMapped.AmbientTypeInfo.Type, ctxType.AmbientTypeInfo.Type } );
                                }
                                else
                                {
                                    var list = interfaceAmbiguities.GetOrSet( itf, t => new List<Type>() { itf, alreadyMapped.AmbientTypeInfo.Type } );
                                    list.Add( ctxType.AmbientTypeInfo.Type );
                                }
                            }
                            else
                            {
                                _mappings.Add( itf, finalType );
                                _mappings.Add( new AmbientContractInterfaceKey( itf ), ctxType );
                            }
                        }
                    }
                }
                var ctxResult = new AmbientContractCollectorContextualResult<CT,T,TC>(
                    Context,
                    _concreteClasses.Select( list => list.ToArray() ).ToArray(),
                    _classAmbiguities != null ? _classAmbiguities : (IReadOnlyList<IReadOnlyList<Type>>)Util.Array.Empty<IReadOnlyList<Type>>(),
                    interfaceAmbiguities != null ? interfaceAmbiguities.Values.Select( list => list.ToArray() ).ToArray() : (IReadOnlyList<IReadOnlyList<Type>>)Util.Array.Empty<IReadOnlyList<Type>>(),
                    _abstractTails );
                return ctxResult;
            }
        }

        /// <summary>
        /// Obtains the result of the collection.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <returns>The result object.</returns>
        public AmbientContractCollectorResult<CT,T,TC> GetResult( IServiceProvider serviceProvider )
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
            var mappings = _mapFactory( _monitor );
            var byContext = new Dictionary<string, PreResult>();
            // Creates default context.
            byContext.Add( string.Empty, new PreResult(
                _monitor,
                mappings.CreateAndAddContext<T,TC>( _monitor, string.Empty ),
                serviceProvider,
                _tempAssembly ) );
            foreach( AmbientTypeInfo m in _roots )
            {
                HandleContexts( m, byContext, mappings.CreateAndAddContext<T,TC>, serviceProvider );
            }
            var r = new AmbientContractCollectorResult<CT,T,TC>( mappings, pocoSupport, _collector, Assemblies );
            foreach( PreResult rCtx in byContext.Values )
            {
                r.Add( rCtx.GetResult( mappings, IsAmbientInterface ) );
            }
            return r;
        }

        bool IsAmbientInterface( Type t )
        {
            Debug.Assert( t.GetTypeInfo().IsInterface );
            return t != typeof( IAmbientContract ) && typeof( IAmbientContract ).IsAssignableFrom( t );
        }

        void HandleContexts( AmbientTypeInfo m, Dictionary<string, PreResult> contexts, Func<IActivityMonitor,string,CT> contextCreator, IServiceProvider services )
        {
            foreach( AmbientTypeInfo child in m.SpecializationsByContext( null ) )
            {
                HandleContexts( child, contexts, contextCreator, services );
                m.MutableFinalContexts.AddRange( child.MutableFinalContexts );
            }
            foreach( string context in m.MutableFinalContexts )
            {
                contexts.GetOrSet( context, c => new PreResult( _monitor, contextCreator( _monitor, c ), services, _tempAssembly ) ).Add( m );
            }
        }

    }


}
