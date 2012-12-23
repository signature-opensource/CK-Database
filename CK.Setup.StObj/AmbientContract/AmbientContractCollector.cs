using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

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
                && (typeof( IAmbientContract ).IsAssignableFrom( t )
                    ||
                   (t.IsClass && typeof( IAmbientContractDefiner ).IsAssignableFrom( t.BaseType )));
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

    public class AmbientContractCollector<T,TC> : AmbientContractCollector
        where T : AmbientTypeInfo
        where TC : AmbientContextTypeInfo<T>
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
        // But for the moment, interfaces can only be marked as Ambient contract statically.
        //
        readonly Dictionary<Type, T> _collector;
        readonly List<T> _roots;
        readonly IAmbientContractDispatcher _contextDispatcher;
        readonly Func<IActivityLogger,T,Type,T> _typeInfoFactory;
        readonly IActivityLogger _logger;
        readonly DynamicAssembly _assembly;

        public AmbientContractCollector( 
            IActivityLogger logger, 
            Func<IActivityLogger,T,Type,T> typeInfoFactory, 
            DynamicAssembly assembly = null, 
            IAmbientContractDispatcher contextDispatcher = null )
        {
            _logger = logger;
            _contextDispatcher = contextDispatcher;
            _assembly = assembly;
            _collector = new Dictionary<Type, T>();
            _roots = new List<T>();
            _typeInfoFactory = typeInfoFactory;
        }

        public int RegisteredTypeCount
        {
            get { return _collector.Count; }
        }

        /// <summary>
        /// Registers multiple types. Only classes are actually registered (the enumearation 
        /// can safely contain null references and interfaces).
        /// </summary>
        /// <param name="types"></param>
        public void Register( IEnumerable<Type> types )
        {
            if( types == null ) throw new ArgumentNullException( "types" );
            foreach( var t in types.Where( c => c != null && c.IsClass && c != typeof(object) ) )
            {
                T result;
                Register( t, out result );
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
            if( !c.IsClass ) throw new ArgumentException();
            T result;
            return c != typeof(object) ? Register( c, out result ) : false;
        }

        bool Register( Type t, out T result )
        {
            Debug.Assert( t != null && t != typeof( object ) && t.IsClass );

            // Skips already processed types.
            if( _collector.TryGetValue( t, out result ) ) return false;

            // Registers parent types whatever they are (null if not AmbientContract).
            T parent = null;
            if( t.BaseType != typeof( object ) ) Register( t.BaseType, out parent );

            // This is an Ambient contract if:
            // - its parent is an ambient contract 
            // - or it is statically an ambient contract (via IAmbientContract support or IAmbientContractDefiner on base class)
            // - or the IAmbientContractDispatcher wants to consider it as one.
            if( parent != null
                || typeof( IAmbientContract ).IsAssignableFrom( t ) || typeof( IAmbientContractDefiner ).IsAssignableFrom( t.BaseType )
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
            T result = _typeInfoFactory( _logger, parent, t );
            if( parent == null ) _roots.Add( result );
            _collector.Add( t, result );
            if( _contextDispatcher != null ) _contextDispatcher.Dispatch( t, result.MutableFinalContexts );
            return result;
        }

        class PreResult
        {
            public readonly string Context;
            readonly IActivityLogger _logger;
            readonly DynamicAssembly _assembly;

            Dictionary<object,TC> _mappings;
            List<List<TC>> _concreteClasses;
            List<IReadOnlyList<Type>> _classAmbiguities;
            List<Type> _abstractTails;
            int _registeredCount;


            public PreResult( IActivityLogger logger, string c, DynamicAssembly assembly )
            {
                Debug.Assert( c != null );
                Context = c;
                _logger = logger;
                _assembly = assembly;
                _mappings = new Dictionary<object, TC>();
                _concreteClasses = new List<List<TC>>();
                _abstractTails = new List<Type>();
            }

            public void Add( AmbientTypeInfo newOne )
            {
                ++_registeredCount;
                if( newOne.Generalization == null )
                {
                    var deepestConcretes = new List<TC>();
                    newOne.CollectDeepestConcrete<T,TC>( _logger, _assembly, deepestConcretes, _abstractTails, Context );
                    if( deepestConcretes.Count == 1 )
                    {
                        var last = deepestConcretes[0];
                        var path = last.CreatePathType( new List<TC>() );
                        _concreteClasses.Add( path );
                        foreach( var m in path ) _mappings.Add( m.AmbientTypeInfo.Type, last );                    
                    }
                    else if( deepestConcretes.Count > 1 )
                    {
                        List<Type> ambiguousPath = new List<Type>() { newOne.Type };
                        ambiguousPath.AddRange( deepestConcretes.Select( m => m.AmbientTypeInfo.Type ) );

                        if( _classAmbiguities == null ) _classAmbiguities = new List<IReadOnlyList<Type>>();
                        _classAmbiguities.Add( ambiguousPath.ToReadOnlyList() );
                    }
                }
            }

            public AmbientContractCollectorContextualResult<T,TC> GetResult( AmbientTypeMapper allMappings, Func<Type, bool> ambientInterfacePredicate )
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
                var ctxMapper = new AmbientTypeContextualMapper<T,TC>( allMappings, Context, _mappings );
                var ctxResult = new AmbientContractCollectorContextualResult<T,TC>(
                    ctxMapper,
                    _concreteClasses.Select( list => list.ToReadOnlyList() ).ToReadOnlyList(),
                    _classAmbiguities != null ? new ReadOnlyListOnIList<IReadOnlyList<Type>>( _classAmbiguities ) : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    interfaceAmbiguities != null ? interfaceAmbiguities.Values.Select( list => list.ToReadOnlyList() ).ToReadOnlyList() : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    new ReadOnlyListOnIList<Type>( _abstractTails ) );
                return ctxResult;
            }
        }

        public AmbientContractCollectorResult<T,TC> GetResult()
        {
            var byContext = new Dictionary<string, PreResult>();
            byContext.Add( String.Empty, new PreResult( _logger, String.Empty, _assembly ) );
            foreach( AmbientTypeInfo m in _roots )
            {
                HandleContexts( _logger, m, _assembly, byContext );
            }
            
            var mappings = new AmbientTypeMapper();
            var r = new AmbientContractCollectorResult<T,TC>( mappings, _collector );
            foreach( var rCtx in byContext.Values )
            {
                r.Add( rCtx.GetResult( mappings, IsAmbientInterface ) );
            }
            return r;
        }

        bool IsAmbientInterface( Type t )
        {
            Debug.Assert( t.IsInterface );
            return t != typeof( IAmbientContract ) && typeof( IAmbientContract ).IsAssignableFrom( t );
        }

        static void HandleContexts( IActivityLogger logger, AmbientTypeInfo m, DynamicAssembly assembly, Dictionary<string, PreResult> contexts )
        {
            foreach( AmbientTypeInfo child in m.SpecializationsByContext( null ) )
            {
                HandleContexts( logger, child, assembly, contexts );
                m.MutableFinalContexts.AddRange( child.MutableFinalContexts );
            }
            foreach( string context in m.MutableFinalContexts )
            {
                contexts.GetOrSet( context, c => new PreResult( logger, c, assembly ) ).Add( m );
            }
        }

    }


}
