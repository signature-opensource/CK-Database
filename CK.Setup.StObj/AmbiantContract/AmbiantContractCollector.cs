using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
    public class AmbiantContractCollector
    {
        readonly Dictionary<Type, AmbiantTypeModel> _collector;
        readonly List<AmbiantTypeModel> _roots;
        readonly IAmbiantContractDispatcher _contextDispatcher;
        
        /// <summary>
        /// Support for <see cref="DefaultContext"/>: a type that defines the notion of default context.
        /// </summary>
        public class DefaultContextType { }

        /// <summary>
        /// The default context contains anything except if a typed context is explicitely defined.
        /// </summary>
        public static readonly Type DefaultContext = typeof( DefaultContextType );

        class AmbiantTypeModel
        {
            public readonly AmbiantTypeModel Parent;
            public readonly Type Type;
            public readonly HashSet<Type> FinalContexts;
            
            readonly AmbiantTypeModel _nextSibling;
            AmbiantTypeModel _firstChild;
            Type[] _ambiantInterfaces;
            Type[] _thisAmbiantInterfaces;

            AmbiantTypeModel( AmbiantTypeModel parent, Type t )
            {
                Type = t;
                FinalContexts = new HashSet<Type>();
                if( (Parent = parent) == null )
                {
                    _nextSibling = null;
                    FinalContexts.Add( AmbiantContractCollector.DefaultContext );
                }
                else
                {
                    FinalContexts.AddRange( Parent.FinalContexts );
                    _nextSibling = Parent._firstChild;
                    Parent._firstChild = this;
                }
                ProcessContextAttributes<AddContextAttribute>( t, FinalContexts.Add );
                ProcessContextAttributes<RemoveContextAttribute>( t, FinalContexts.Remove );
            }

            public Type[] AmbiantInterfaces
            {
                get { return _ambiantInterfaces ?? (_ambiantInterfaces = Type.GetInterfaces().Where( t => t != typeof(IAmbiantContract) && (typeof(IAmbiantContract).IsAssignableFrom( t ) ) ).ToArray()); }
            }

            public Type[] ThisAmbiantInterfaces
            {
                get { return _thisAmbiantInterfaces ?? (_thisAmbiantInterfaces = Parent != null ? AmbiantInterfaces.Except( Parent.AmbiantInterfaces ).ToArray() : AmbiantInterfaces); }
            }

            public IEnumerable<AmbiantTypeModel> EnumChild( Type context = null )
            {
                AmbiantTypeModel c = _firstChild;
                while( c != null )
                {
                    if( context == null || c.FinalContexts.Contains( context ) ) yield return c;
                    c = c._nextSibling;
                }
            }

            public bool CollectDeepestConcrete( List<AmbiantTypeModel> lastConcretes, List<Type> abstractTails, Type context = null )
            {
                bool concreteBelow = false;
                AmbiantTypeModel c = _firstChild;
                while( c != null )
                {
                    if( context == null || c.FinalContexts.Contains( context ) )
                    {
                        concreteBelow |= c.CollectDeepestConcrete( lastConcretes, abstractTails, context );
                    }
                    c = c._nextSibling;
                }
                if( !concreteBelow )
                {
                    if( Type.IsAbstract )
                    {
                        abstractTails.Add( Type );
                    }
                    else
                    {
                        lastConcretes.Add( this );
                        concreteBelow = true;
                    }
                }
                return concreteBelow;
            }

            static void ProcessContextAttributes<T>( Type t, Func<Type, bool> action ) where T : IContextDefiner
            {
                object[] attrs = t.GetCustomAttributes( typeof( T ), false );
                foreach( var a in attrs ) action( ((IContextDefiner)a).Context );
            }

            public static bool Register( AmbiantContractCollector collector, Type t, out AmbiantTypeModel result )
            {
                Debug.Assert( t != null && t != typeof( object ) && t.IsClass );
                
                // Skips already processed types.
                if( collector._collector.TryGetValue( t, out result ) ) return false;
                
                // Registers parent types whatever they are (null if not AmbiantContract).
                AmbiantTypeModel parent = null;
                if( t.BaseType != typeof(object) ) Register( collector, t.BaseType, out parent );

                if( typeof( IAmbiantContract ).IsAssignableFrom( t ) || typeof( IAmbiantContractDefiner ).IsAssignableFrom( t.BaseType ) )
                {
                    Debug.Assert( AmbiantContractCollector.IsAmbiantContract( t ) );
                    result = new AmbiantTypeModel( parent, t );
                    if( parent == null ) collector._roots.Add( result );
                    collector._collector.Add( t, result );
                    if( collector._contextDispatcher != null ) collector._contextDispatcher.Dispatch( t, result.FinalContexts );
                }
                else
                {
                    Debug.Assert( AmbiantContractCollector.IsAmbiantContract( t ) == false );
                    // Marks the type as a registered one.
                    collector._collector.Add( t, null );
                }
                return true;
            }


            internal List<AmbiantTypeModel> FillPath( List<AmbiantTypeModel> path )
            {
                if( Parent != null ) Parent.FillPath( path );
                path.Add( this );
                return path;
            }
        }

        public AmbiantContractCollector( IAmbiantContractDispatcher contextDispatcher = null )
        {
            _contextDispatcher = contextDispatcher;
            _collector = new Dictionary<Type, AmbiantTypeModel>();
            _roots = new List<AmbiantTypeModel>();
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
                AmbiantTypeModel result;
                AmbiantTypeModel.Register( this, t, out result );
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
            AmbiantTypeModel result;
            return c != typeof(object) ? AmbiantTypeModel.Register( this, c, out result ) : false;
        }

        class PreResult
        {
            public readonly Type Context;

            Dictionary<object,Type> _mappings;
            List<List<AmbiantTypeModel>> _concreteClasses;
            List<IReadOnlyList<Type>> _classAmbiguities;
            List<Type> _abstractTails;
            int _registeredCount;

            public PreResult( Type c )
            {
                Context = c;
                _mappings = new Dictionary<object, Type>();
                _concreteClasses = new List<List<AmbiantTypeModel>>();
                _abstractTails = new List<Type>();
            }

            public void Add( AmbiantTypeModel newOne )
            {
                ++_registeredCount;
                if( newOne.Parent == null )
                {
                    List<AmbiantTypeModel> deepestConcretes = new List<AmbiantTypeModel>();
                    newOne.CollectDeepestConcrete( deepestConcretes, _abstractTails, Context );
                    if( deepestConcretes.Count == 1 )
                    {
                        var last = deepestConcretes[0];
                        var path = last.FillPath( new List<AmbiantTypeModel>() );
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

            public AmbiantContractCollectorContextualResult GetResult( AmbiantTypeMapper allMappings )
            {
                Dictionary<Type,List<Type>> interfaceAmbiguities = null;
                foreach( var path in _concreteClasses )
                {
                    var finalType = path[path.Count - 1].Type;
                    foreach( AmbiantTypeModel m in path )
                    {
                        foreach( Type itf in m.ThisAmbiantInterfaces )
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
                                _mappings.Add( new AmbiantContractInterfaceKey( itf ), m.Type );
                            }
                        }
                    }
                }
                AmbiantTypeContextualMapper ctxMapper = new AmbiantTypeContextualMapper( allMappings, Context, _mappings );
                AmbiantContractCollectorContextualResult ctxResult = new AmbiantContractCollectorContextualResult(
                    ctxMapper,
                    _concreteClasses.Select( list => list.Select( m => m.Type ).ToReadOnlyList() ).ToReadOnlyList(),
                    _classAmbiguities != null ? new ReadOnlyListOnIList<IReadOnlyList<Type>>( _classAmbiguities ) : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    interfaceAmbiguities != null ? interfaceAmbiguities.Values.Select( list => list.ToReadOnlyList() ).ToReadOnlyList() : ReadOnlyListEmpty<IReadOnlyList<Type>>.Empty,
                    new ReadOnlyListOnIList<Type>( _abstractTails ) );
                return ctxResult;
            }
        }

        public AmbiantContractCollectorResult GetResult()
        {
            Dictionary<Type,PreResult> byContext = new Dictionary<Type, PreResult>();
            byContext.Add( AmbiantContractCollector.DefaultContext, new PreResult( AmbiantContractCollector.DefaultContext ) );
            foreach( AmbiantTypeModel m in _roots )
            {
                HandleContexts( m, byContext );
            }
            
            var mappings = new AmbiantTypeMapper();
            var r = new AmbiantContractCollectorResult( mappings );
            foreach( var rCtx in byContext.Values )
            {
                r.Add( rCtx.GetResult( mappings ) );
            }
            return r;
        }

        static void HandleContexts( AmbiantTypeModel m, Dictionary<Type, PreResult> contexts )
        {
            foreach( AmbiantTypeModel child in m.EnumChild() )
            {
                HandleContexts( child, contexts );
                m.FinalContexts.AddRange( child.FinalContexts );
            }
            foreach( Type context in m.FinalContexts )
            {
                contexts.GetOrSet( context, c => new PreResult( c ) ).Add( m );
            }
        }

        /// <summary>
        /// Formats a string that combines a context and a type information.
        /// </summary>
        /// <param name="context">Context can be null (considered as <see cref="DefaultContext"/>).</param>
        /// <param name="type">Type for which a display name must be obtained.</param>
        /// <returns>Human readable name for the type in context.</returns>
        static public string DisplayName( Type context, Type type )
        {
            return context == null || context == AmbiantContractCollector.DefaultContext
                ? type.FullName
                : "[" + context.Name + "]" + type.FullName;
        }

        /// <summary>
        /// Tests whether a Type is an <see cref="IAmbiantContract"/>.
        /// It applies to interfaces and classes (for a class <see cref="IAmbiantContractDefiner"/> is 
        /// checked on its base class).
        /// </summary>
        /// <param name="t">Type to challenge.</param>
        /// <returns>True if the type is an ambiant contract.</returns>
        static public bool IsAmbiantContract( Type t )
        {
            return 
                t != null 
                && t != typeof( object )
                && (typeof( IAmbiantContract ).IsAssignableFrom( t ) 
                    || 
                   (t.IsClass && typeof( IAmbiantContractDefiner ).IsAssignableFrom( t.BaseType )));
        }
    }

}
