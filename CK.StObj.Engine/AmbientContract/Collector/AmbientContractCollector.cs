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
    /// Discovers types that support <see cref="IAmbientContract"/> marker interface and manages to 
    /// dispatch them among different contexts (identified by a string) with generalization/specialization handling.
    /// </summary>
    /// <remarks>
    /// The default context is identified by the empty string and contains all <see cref="IAmbientContract"/> that are 
    /// not explicitely associated to a specific context.
    /// </remarks>
    public class AmbientContractCollector
    {
        /// <summary>
        /// Tests whether a Type is an <see cref="IAmbientContract"/>.
        /// It applies to interfaces and classes (for a class <see cref="IAmbientContractDefiner"/> is 
        /// checked on its base class).
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if the type is an ambient contract.</returns>
        static public bool IsStaticallyTypedAmbientContract( Type t )
        {
            return
                t != null
                && t != typeof( object )
                && (typeof( IAmbientContract ).GetTypeInfo().IsAssignableFrom( t )
                    ||
                   (t.GetTypeInfo().IsClass && typeof( IAmbientContractDefiner ).IsAssignableFrom( t.GetTypeInfo().BaseType )));
        }

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
        
    }

    /// <summary>
    /// Typed implementation of an <see cref="AmbientContractCollector"/>.
    /// </summary>
    /// <typeparam name="CT">A <see cref="AmbientContextualTypeMap{T,TC}"/> type.</typeparam>
    /// <typeparam name="T">A <see cref="AmbientTypeInfo"/> type.</typeparam>
    /// <typeparam name="TC">A <see cref="AmbientContextualTypeInfo{T,TC}"/> type.</typeparam>
    public class AmbientContractCollector<CT,T,TC> : AmbientContractCollector
        where CT : AmbientContextualTypeMap<T, TC>
        where T : AmbientTypeInfo
        where TC : AmbientContextualTypeInfo<T, TC>
    {
        // Today, this collector contains 2 kind of information:
        // - Type Classes mapped to their AmbientTypeInfo if considered as an Ambient contract.
        // - Type Classes mapped to null when known to be NOT an Ambient contract (not statically typed nor "saved" by IAmbientContractDispatcher.IsAmbientContractClass).
        //
        // Tomorrow, it may contain:
        // - Type Interfaces mapped to a special AmbientTypeInfo.InterfaceIsAnAmbientContract for interfaces considered as an Ambient contract.
        // - Type Interfaces mapped to a special AmbientTypeInfo.InterfaceMustBeMappedInContext for interfaces that are not considered as Ambient contract (in the sense where
        //   they do not make classes that implement them Ambient contract classes nor do they make their specialized interfaces Ambient contracts).
        // - Type Interfaces mapped to null for interfaces that we already know as beeing "totally normal".
        //
        // But for the moment, interfaces can only be marked as Ambient Contract statically.
        //
        readonly Dictionary<Type, T> _collector;
        readonly List<T> _roots;
        readonly IAmbientContractDispatcher _contextDispatcher;

        readonly Func<IActivityMonitor, AmbientTypeMap<CT>> _mapFactory;
        readonly Func<IActivityMonitor,T,Type,T> _typeInfoFactory;
        
        readonly IActivityMonitor _monitor;
        readonly DynamicAssembly _tempAssembly;
        readonly DynamicAssembly _finalAssembly;
        readonly PocoRegisterer _pocoRegisterer;

        /// <summary>
        /// Initializes a new <see cref="AmbientContractCollector{CT,T,TC}"/> instance.
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="mapFactory">Factory for <see cref="IContextualTypeMap"/> objects.</param>
        /// <param name="typeInfoFactory">Factory for <see cref="AmbientTypeInfo"/> objects.</param>
        /// <param name="tempAssembly">The temporary <see cref="DynamicAssembly"/>.</param>
        /// <param name="finalAssembly">The optional final <see cref="DynamicAssembly"/>.</param>
        /// <param name="contextDispatcher">The strategy that will be used to alter type dispatching.</param>
        public AmbientContractCollector( 
            IActivityMonitor monitor,
            Func<IActivityMonitor, AmbientTypeMap<CT>> mapFactory,
            Func<IActivityMonitor, T, Type, T> typeInfoFactory,
            DynamicAssembly tempAssembly,
            DynamicAssembly finalAssembly = null,
            IAmbientContractDispatcher contextDispatcher = null )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( mapFactory == null ) throw new ArgumentNullException( nameof( mapFactory ) );
            if( typeInfoFactory == null ) throw new ArgumentNullException( nameof( typeInfoFactory ) );
            if( tempAssembly == null ) throw new ArgumentNullException( nameof( tempAssembly ) );
            _monitor = monitor;
            _contextDispatcher = contextDispatcher;
            _tempAssembly = tempAssembly;
            _finalAssembly = finalAssembly;
            _collector = new Dictionary<Type, T>();
            _roots = new List<T>();
            _mapFactory = mapFactory;
            _typeInfoFactory = typeInfoFactory;
            _pocoRegisterer = new PocoRegisterer();
        }

        /// <summary>
        /// Gets the number of registered types.
        /// </summary>
        public int RegisteredTypeCount => _collector.Count; 

        /// <summary>
        /// Registers multiple types. Classes that are registered (the enumeration 
        /// can safely contain null references).
        /// </summary>
        /// <param name="types"></param>
        public void Register( IEnumerable<Type> types )
        {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( var t in types )
            {
                if( t != typeof( object ) )
                {
                    if( t.GetTypeInfo().IsClass )
                    {
                        T result;
                        DoRegisterClass( t, out result );
                    }
                    else if( t.GetTypeInfo().IsInterface && typeof(IPoco).IsAssignableFrom( t ) )
                    {
                        _pocoRegisterer.Register( _monitor, t );
                    }
                }
            }
        }

        /// <summary>
        /// Registers a class.
        /// </summary>
        /// <param name="c">Class to register.</param>
        /// <returns>True if it is a new class for this collector, false if it has already been registered.</returns>
        public bool RegisterClass( Type c )
        {
            if( c == null ) throw new ArgumentNullException( "c" );
            if( !c.GetTypeInfo().IsClass ) throw new ArgumentException();
            T result;
            return c != typeof(object) ? DoRegisterClass( c, out result ) : false;
        }

        bool DoRegisterClass( Type t, out T result )
        {
            Debug.Assert( t != null && t != typeof( object ) && t.GetTypeInfo().IsClass );

            // Skips already processed types.
            if( _collector.TryGetValue( t, out result ) ) return false;

            // Registers parent types whatever they are (null if not AmbientContract).
            T parent = null;
            if( t.GetTypeInfo().BaseType != typeof( object ) ) DoRegisterClass( t.GetTypeInfo().BaseType, out parent );

            // This is an Ambient contract if:
            // - its parent is an ambient contract 
            // - or it is statically an ambient contract (via IAmbientContract support or IAmbientContractDefiner on base class)
            // - or the IAmbientContractDispatcher wants to consider it as one.
            if( parent != null
                || typeof( IAmbientContract ).IsAssignableFrom( t ) || typeof( IAmbientContractDefiner ).IsAssignableFrom( t.GetTypeInfo().BaseType )
                || (_contextDispatcher != null && _contextDispatcher.IsAmbientContractClass( t )) )
            {
                result = CreateTypeInfo( t, parent );
            }
            else
            {
                Debug.Assert( AmbientContractCollector.IsStaticallyTypedAmbientContract( t ) == false );
                // Marks the type as a registered one.
                _collector.Add( t, null );
            }
            return true;
        }

        T CreateTypeInfo( Type t, T parent )
        {
            T result = _typeInfoFactory( _monitor, parent, t );
            if( parent == null ) _roots.Add( result );
            _collector.Add( t, result );
            if( _contextDispatcher != null ) _contextDispatcher.Dispatch( t, result.MutableFinalContexts );
            return result;
        }

        class PreResult
        {
            public readonly CT Context;
            readonly IActivityMonitor _monitor;
            readonly DynamicAssembly _tempAssembly;

            Dictionary<object,TC> _mappings;
            List<List<TC>> _concreteClasses;
            List<IReadOnlyList<Type>> _classAmbiguities;
            List<Type> _abstractTails;
            int _registeredCount;

            public PreResult( IActivityMonitor monitor, CT c, DynamicAssembly tempAssembly )
            {
                Debug.Assert( c != null );
                Context = c;
                _monitor = monitor;
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
                    newOne.CollectDeepestConcrete<T, TC>( _monitor, Context, null, _tempAssembly, deepestConcretes, _abstractTails );
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
        /// <returns>The result object.</returns>
        public AmbientContractCollectorResult<CT,T,TC> GetResult()
        {
            IPocoSupportResult pocoSupport;
            using( _monitor.OpenInfo().Send( "Creating Poco Types and PocoFactory." ) )
            {
                pocoSupport = _pocoRegisterer.Finalize( _finalAssembly ?? _tempAssembly, _monitor );
                if( pocoSupport != null ) RegisterClass( pocoSupport.FinalFactory );
            }
            var mappings = _mapFactory( _monitor );
            var byContext = new Dictionary<string, PreResult>();
            byContext.Add( string.Empty, new PreResult( _monitor, mappings.CreateAndAddContext<T,TC>( _monitor, string.Empty ), _tempAssembly ) );
            foreach( AmbientTypeInfo m in _roots )
            {
                HandleContexts( m, byContext, mappings.CreateAndAddContext<T,TC> );
            }
            var r = new AmbientContractCollectorResult<CT,T,TC>( mappings, pocoSupport, _collector );
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

        void HandleContexts( AmbientTypeInfo m, Dictionary<string, PreResult> contexts, Func<IActivityMonitor,string,CT> contextCreator )
        {
            foreach( AmbientTypeInfo child in m.SpecializationsByContext( null ) )
            {
                HandleContexts( child, contexts, contextCreator );
                m.MutableFinalContexts.AddRange( child.MutableFinalContexts );
            }
            foreach( string context in m.MutableFinalContexts )
            {
                contexts.GetOrSet( context, c => new PreResult( _monitor, contextCreator( _monitor, c ), _tempAssembly ) ).Add( m );
            }
        }

    }


}
