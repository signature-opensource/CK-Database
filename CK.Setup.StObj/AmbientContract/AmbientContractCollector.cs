using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
    public class AmbientContractCollector
    {
        /// <summary>
        /// Support for <see cref="DefaultContext"/>: a type that defines the notion of default context.
        /// </summary>
        public class DefaultContextType { }

        /// <summary>
        /// The default context contains anything except if a typed context is explicitely defined.
        /// </summary>
        public static readonly Type DefaultContext = typeof( DefaultContextType );


        /// <summary>
        /// Formats a string that combines a context and a type information.
        /// </summary>
        /// <param name="context">Context can be null (considered as <see cref="DefaultContext"/>).</param>
        /// <param name="type">Type for which a display name must be obtained.</param>
        /// <returns>Human readable name for the type in context.</returns>
        static public string DisplayName( Type context, Type type )
        {
            if( type == null ) throw new ArgumentNullException( "type" );
            return DisplayName( context, type.FullName );
        }

        /// <summary>
        /// Formats a string that combines a context and the name of something.
        /// </summary>
        /// <param name="context">Context can be null (considered as <see cref="DefaultContext"/>).</param>
        /// <param name="fullName">A name that must be contextualized.</param>
        /// <returns>Human readable name for the name in context.</returns>
        static public string DisplayName( Type context, string fullName )
        {
            if( fullName == null ) throw new ArgumentNullException( "fullName" );
            return context == null || context == AmbientContractCollector.DefaultContext
                ? fullName
                : "[" + context.Name + "]" + fullName;
        }

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
    }

    public class AmbientContractCollector<TAmbientTypeInfo> : AmbientContractCollector
        where TAmbientTypeInfo : AmbientTypeInfo
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
        readonly Dictionary<Type, TAmbientTypeInfo> _collector;
        readonly List<TAmbientTypeInfo> _roots;
        readonly IAmbientContractDispatcher _contextDispatcher;
        readonly Func<IActivityLogger,TAmbientTypeInfo,Type,TAmbientTypeInfo> _typeInfoFactory;
        readonly IActivityLogger _logger;

        public AmbientContractCollector( IActivityLogger logger, Func<IActivityLogger,TAmbientTypeInfo,Type,TAmbientTypeInfo> typeInfoFactory, IAmbientContractDispatcher contextDispatcher = null )
        {
            _logger = logger;
            _contextDispatcher = contextDispatcher;
            _collector = new Dictionary<Type, TAmbientTypeInfo>();
            _roots = new List<TAmbientTypeInfo>();
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
                TAmbientTypeInfo result;
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
            TAmbientTypeInfo result;
            return c != typeof(object) ? Register( c, out result ) : false;
        }

        bool Register( Type t, out TAmbientTypeInfo result )
        {
            Debug.Assert( t != null && t != typeof( object ) && t.IsClass );

            // Skips already processed types.
            if( _collector.TryGetValue( t, out result ) ) return false;

            // Registers parent types whatever they are (null if not AmbientContract).
            TAmbientTypeInfo parent = null;
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

        TAmbientTypeInfo CreateTypeInfo( Type t, TAmbientTypeInfo parent )
        {
            TAmbientTypeInfo result = _typeInfoFactory( _logger, parent, t );
            if( parent == null ) _roots.Add( result );
            _collector.Add( t, result );
            if( _contextDispatcher != null ) _contextDispatcher.Dispatch( t, result.MutableFinalContexts );
            return result;
        }

        class PreResult
        {
            public readonly Type Context;

            Dictionary<object,Type> _mappings;
            List<List<TAmbientTypeInfo>> _concreteClasses;
            List<IReadOnlyList<Type>> _classAmbiguities;
            List<Type> _abstractTails;
            int _registeredCount;

            public PreResult( Type c )
            {
                Context = c;
                _mappings = new Dictionary<object, Type>();
                _concreteClasses = new List<List<TAmbientTypeInfo>>();
                _abstractTails = new List<Type>();
            }

            public void Add( AmbientTypeInfo newOne )
            {
                ++_registeredCount;
                if( newOne.Generalization == null )
                {
                    List<AmbientTypeInfo> deepestConcretes = new List<AmbientTypeInfo>();
                    newOne.CollectDeepestConcrete( deepestConcretes, _abstractTails, Context );
                    if( deepestConcretes.Count == 1 )
                    {
                        var last = deepestConcretes[0];
                        var path = last.FillPath( new List<TAmbientTypeInfo>() );
                        _concreteClasses.Add( path );
                        foreach( var m in path ) _mappings.Add( m.Type, last.Type );                    
                    }
                    else if( deepestConcretes.Count > 1 )
                    {
                        deepestConcretes.Insert( 0, newOne );
                        if( _classAmbiguities == null ) _classAmbiguities = new List<IReadOnlyList<Type>>();
                        _classAmbiguities.Add( deepestConcretes.Select( m => m.Type ).ToReadOnlyList() );
                    }
                }
            }

            public AmbientContractCollectorContextualResult<TAmbientTypeInfo> GetResult( AmbientTypeMapper allMappings, Func<Type, bool> ambientInterfacePredicate )
            {
                Dictionary<Type,List<Type>> interfaceAmbiguities = null;
                foreach( var path in _concreteClasses )
                {
                    var finalType = path[path.Count - 1].Type;
                    foreach( AmbientTypeInfo m in path )
                    {
                        foreach( Type itf in m.EnsureThisAmbientInterfaces( ambientInterfacePredicate ) )
                        {
                            Type alreadyMapped;
                            if( _mappings.TryGetValue( itf, out alreadyMapped ) )
                            {
                                if( interfaceAmbiguities == null ) 
                                {
                                    interfaceAmbiguities = new Dictionary<Type,List<Type>>();
                                    interfaceAmbiguities.Add( itf, new List<Type>() { itf, alreadyMapped, m.Type } );
                                }
                                else
                                {
                                    var list = interfaceAmbiguities.GetOrSet( itf, t => new List<Type>() { itf, alreadyMapped } );
                                    list.Add( m.Type );
                                }
                            }
                            else
                            {
                                _mappings.Add( itf, finalType );
                                _mappings.Add( new AmbientContractInterfaceKey( itf ), m.Type );
                            }
                        }
                    }
                }
                AmbientTypeContextualMapper ctxMapper = new AmbientTypeContextualMapper( allMappings, Context, _mappings );
                var ctxResult = new AmbientContractCollectorContextualResult<TAmbientTypeInfo>(
                    ctxMapper,
                    _concreteClasses.Select( list => list.ToReadOnlyList() ).ToReadOnlyList(),
                    _classAmbiguities != null ? new ReadOnlyListOnIList<IReadOnlyList<Type>>( _classAmbiguities ) : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    interfaceAmbiguities != null ? interfaceAmbiguities.Values.Select( list => list.ToReadOnlyList() ).ToReadOnlyList() : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    new ReadOnlyListOnIList<Type>( _abstractTails ) );
                return ctxResult;
            }
        }

        public AmbientContractCollectorResult<TAmbientTypeInfo> GetResult()
        {
            Dictionary<Type,PreResult> byContext = new Dictionary<Type, PreResult>();
            byContext.Add( AmbientContractCollector.DefaultContext, new PreResult( AmbientContractCollector.DefaultContext ) );
            foreach( AmbientTypeInfo m in _roots )
            {
                HandleContexts( m, byContext );
            }
            
            var mappings = new AmbientTypeMapper();
            var r = new AmbientContractCollectorResult<TAmbientTypeInfo>( mappings, _collector );
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

        static void HandleContexts( AmbientTypeInfo m, Dictionary<Type, PreResult> contexts )
        {
            foreach( AmbientTypeInfo child in m.SpecializationsByContext( null ) )
            {
                HandleContexts( child, contexts );
                m.MutableFinalContexts.AddRange( child.MutableFinalContexts );
            }
            foreach( Type context in m.MutableFinalContexts )
            {
                contexts.GetOrSet( context, c => new PreResult( c ) ).Add( m );
            }
        }

    }


}
