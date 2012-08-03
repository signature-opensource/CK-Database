using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
    public class AmbiantContractCollector
    {
        HashSet<Type> _processed;
        readonly IAmbiantContractContextMapper _contextMapper;
        bool _processedResultFlag;

        class ClassType : List<Type>
        {
            public ClassType( IAmbiantContractContextMapper contextMapper, Type final )
            {
                bool hasDefiner = typeof( IAmbiantContractDefiner ).IsAssignableFrom( final );
                bool hasAmbiant = typeof( IAmbiantContract ).IsAssignableFrom( final );
                
                Debug.Assert( hasAmbiant || hasDefiner );
                
                Type nextFinal;
                Type[] nextFinalInterfaces = null;
                Type[] finalInterfaces = final.GetInterfaces();
                // Handles current Context and its holder (the type in the inheritance chain that defined it).
                Type context = null;
                Type contextHolder = null;
                bool hasBeenExplicitelyContextualized = false;
                while( (nextFinal = final.BaseType) != null && (hasAmbiant || hasDefiner) )
                {
                    // Explicit Context association:
                    if( !hasBeenExplicitelyContextualized && contextMapper != null )
                    {
                        // This works because a IAmbiantContractContextMapper MUST guaranty
                        // that no ambiguities can exist in an inheritance chain:
                        // Whatever T1, T2 are if T1 and T2 are both mapped and T1 is an ancestor of T2, then T1 and T2 are mapped 
                        // to the same context (be it the null one).
                        hasBeenExplicitelyContextualized = contextMapper.FindExplicitContext( final, ref context );
                    }
                    nextFinalInterfaces = nextFinal.GetInterfaces();
                    var thisLevel = finalInterfaces.Except( nextFinalInterfaces );
                    if( hasAmbiant )
                    {
                        if( !hasBeenExplicitelyContextualized )
                        {
                            ExtractContext( typeof( IAmbiantContract<> ), thisLevel, final, ref context, ref contextHolder );
                        }
                    }
                    bool nextHasDefiner = hasDefiner;
                    if( hasDefiner )
                    {
                        if( !hasBeenExplicitelyContextualized )
                        {
                            ExtractContext( typeof( IAmbiantContractDefiner<> ), thisLevel, final, ref context, ref contextHolder );
                        }
                        nextHasDefiner = typeof( IAmbiantContractDefiner ).IsAssignableFrom( nextFinal );
                    }
                    if( hasAmbiant || nextHasDefiner ) Add( final );
                    final = nextFinal;
                    finalInterfaces = nextFinalInterfaces;
                    hasAmbiant &= typeof( IAmbiantContract ).IsAssignableFrom( final );
                    hasDefiner = nextHasDefiner;
                }
                Reverse();
                Context = context;
                IndexOfFinalConcrete = Count - 1;
                while( IndexOfFinalConcrete >= 0 && this[IndexOfFinalConcrete].IsAbstract ) --IndexOfFinalConcrete;
            }

            private static void ExtractContext( Type markerType, IEnumerable<Type> thisLevel, Type final, ref Type context, ref Type contextHolder )
            {
                var ambiants = thisLevel.Where( t => t.IsGenericType && t.GetGenericTypeDefinition() == markerType );
                var theOne = ambiants.SingleOrDefault();
                if( theOne != null )
                {
                    if( context != null )
                    {
                        // Allowing a Context to be redefined would easily work if we were sure
                        // that a Type is always discovered AFTER any of its specialization...
                        // This is because we register the abstractions/inheritance chain immediatelty after
                        // the type discovering.
                        throw new CKException( "Ambiant contract context '{0}' is defined on class '{1}', but class '{2}' redefines it as '{3}'. Context redefinition is not supported.",
                            theOne.GetGenericArguments()[0].Name, final.FullName, contextHolder.FullName, context.Name );
                    }
                    context = theOne.GetGenericArguments()[0];
                    contextHolder = final;
                }
                else
                {
                    if( ambiants.Count() > 1 ) throw new CKException( "Multiple definition of {0} marker on class '{1}'.", markerType.Name, final.FullName );
                }
            }

            public bool IsPureDefiner { get { return Count == 0; } }
            public Type Context { get; private set; }
            public Type Head { get { return this[0]; } }
            public Type Final { get { return this[Count - 1]; } }
            public readonly int IndexOfFinalConcrete;
            public bool HasFinalConcrete { get { return IndexOfFinalConcrete >= 0; } }
            public bool HasAbstractTail { get { return IndexOfFinalConcrete < Count - 1; } }
            public Type FinalConcrete { get { return this[IndexOfFinalConcrete]; } }
            public IEnumerable<Type> ToFinalConcrete { get { return this.Take( IndexOfFinalConcrete + 1 ); } }
            public IEnumerable<Type> AbstractTail { get { return this.Skip( IndexOfFinalConcrete ); } }

            public bool ProcessedResultFlag;
        }

        #region PreContext

        class PreContext
        {
            Type _context;
            Dictionary<Type,ClassType> _classMap;
            int _regTypeCount;
            List<ClassType> _classMapAmbiguities;
            List<Type> _pureDefiners;
            
            internal PreContext( Type t )
            {
                _context = t;
                _classMap = new Dictionary<Type, ClassType>();
            }

            internal bool Register( ClassType cNew, Type c )
            {
                Debug.Assert( cNew.Context == _context );
                if( cNew.IsPureDefiner )
                {
                    if( _pureDefiners == null ) _pureDefiners = new List<Type>();
                    ++_regTypeCount;
                    _pureDefiners.Add( c );
                    return true;
                }
                int deltaNew;
                ClassType cPrv;
                if( _classMap.TryGetValue( cNew.Head, out cPrv ) )
                {
                    deltaNew = cNew.Count - cPrv.Count;
                    int lastCommonIdx = (deltaNew <= 0 ? cNew.Count : cPrv.Count) - 1;
                    if( cPrv[lastCommonIdx] != cNew[lastCommonIdx] )
                    {
                        if( _classMapAmbiguities == null ) _classMapAmbiguities = new List<ClassType>();
                        _classMapAmbiguities.Add( cNew );
                        return false;
                    }
                    if( deltaNew <= 0 )
                    {
                        // The new path is smaller than the existing one (a prefix of the existing one). 
                        return false;
                    }
                }
                else deltaNew = cNew.Count;

                foreach( var t in cNew ) _classMap[t] = cNew;
                _regTypeCount += deltaNew;
                return true;
            }

            internal int RegisteredTypeCount 
            { 
                get { return _regTypeCount; } 
            }

            internal AmbiantContractResult GetResult( bool processedResultFlag )
            {
                Dictionary<object,Type> mappings = new Dictionary<object, Type>();
                List<IReadOnlyList<Type>> concreteClassesPath = new List<IReadOnlyList<Type>>();
                List<Tuple<Type,Type>> itfA = null;
                List<IReadOnlyList<Type>> abstractClasses = null;
                List<IReadOnlyList<Type>> abstractTails = null;
                
                foreach( ClassType ct in _classMap.Values )
                {
                    if( ct.ProcessedResultFlag == processedResultFlag ) continue;
                    ct.ProcessedResultFlag = processedResultFlag;

                    if( ct.HasFinalConcrete )
                    {
                        Type fc = ct.FinalConcrete;
                        IReadOnlyList<Type> pathConcrete = ct.ToFinalConcrete.ToReadOnlyList();
                        concreteClassesPath.Add( pathConcrete );
                        foreach( Type mapped in pathConcrete ) mappings.Add( mapped, fc );
                        foreach( Type iFace in fc.GetInterfaces() )
                        {
                            if( typeof( IAmbiantContract ).IsAssignableFrom( iFace ) 
                                && iFace != typeof( IAmbiantContract ) 
                                && !(iFace.IsGenericType && iFace.GetGenericTypeDefinition() == typeof(IAmbiantContract<>)))
                            {
                                Type alreadyMapped = mappings.GetValueWithDefault( iFace, null );
                                if( alreadyMapped != null )
                                {
                                    if( itfA == null ) itfA = new List<Tuple<Type, Type>>();
                                    itfA.Add( Tuple.Create( iFace, alreadyMapped ) );
                                }
                                else
                                {
                                    // Looking for highest (most general) implementation 
                                    // of iFace.
                                    Type highestImpl = fc;
                                    Debug.Assert( iFace.IsAssignableFrom( highestImpl ), "The final concrete implements iFace." );
                                    for( int i = pathConcrete.Count-2; i >= 0; --i )
                                    {
                                        Type candidate = pathConcrete[i];
                                        if( iFace.IsAssignableFrom( candidate ) ) highestImpl = candidate;
                                        else break;
                                    }
                                    mappings.Add( new AmbiantContractInterfaceKey( iFace ), highestImpl );
                                    // Adds the mapping from the interface to the 
                                    // final concrete class.
                                    mappings.Add( iFace, fc );
                                }
                            }
                        }
                        if( ct.HasAbstractTail )
                        {
                            if( abstractTails == null ) abstractTails = new List<IReadOnlyList<Type>>();
                            abstractTails.Add( ct.AbstractTail.ToReadOnlyList() );
                        }
                    }
                    else
                    {
                        if( abstractClasses == null ) abstractClasses = new List<IReadOnlyList<Type>>();
                        abstractClasses.Add( ct.ToReadOnlyList() );
                    }
                }
                IReadOnlyList<Tuple<Type,Type>> clsA = _classMapAmbiguities == null
                    ? ReadOnlyListEmpty<Tuple<Type, Type>>.Empty
                    : _classMapAmbiguities.Select( ct => Tuple.Create( ct.Head, ct.Final ) ).ToReadOnlyList();

                return new AmbiantContractResult( _context, mappings, concreteClassesPath.ToReadOnlyList(), clsA, itfA, abstractClasses, abstractTails, _pureDefiners );
            }

        }

        PreContext _defaultCtx;
        Dictionary<Type,PreContext> _ctx;

        PreContext GetContext( Type t )
        {
            PreContext result = _defaultCtx;
            if( t != null )
            {
                if( _ctx == null ) _ctx = new Dictionary<Type, PreContext>();
                else if( _ctx.TryGetValue( t, out result ) ) return result;
                result = new PreContext( t );
                _ctx.Add( t, result );
            }
            return result;
        }
        #endregion

        public AmbiantContractCollector( IAmbiantContractContextMapper contextMapper = null )
        {
            _defaultCtx = new PreContext( null );
            _ctx = new Dictionary<Type, PreContext>();
            _processed = new HashSet<Type>();
            _contextMapper = contextMapper;
        }

        public int RegisteredTypeCount
        {
            get { return _defaultCtx.RegisteredTypeCount + _ctx.Values.Sum( c => c.RegisteredTypeCount ); }
        }

        public void Register( IEnumerable<Type> types, Action<Type> onClassRegistered = null )
        {
            foreach( var t in types.Where( c => c.IsClass
                                                && (typeof( IAmbiantContract ).IsAssignableFrom( c ) || typeof( IAmbiantContractDefiner ).IsAssignableFrom( c ))
                                                && !_processed.Contains( c ) ) )
            {
                if( DoReg( t ) && onClassRegistered != null ) onClassRegistered( t );
            }
        }

        public bool RegisterClass( Type c )
        {
            if( c == null ) throw new ArgumentNullException();
            if( !c.IsClass ) throw new ArgumentException();
            if( _processed.Contains( c ) ) return false;
            if( (typeof( IAmbiantContract ).IsAssignableFrom( c ) || typeof( IAmbiantContractDefiner ).IsAssignableFrom( c )) )
            {
                return DoReg( c );
            }
            return false;
        }

        bool DoReg( Type c )
        {
            _processed.Add( c );
            ClassType cNew = new ClassType( _contextMapper, c ) { ProcessedResultFlag = _processedResultFlag };
            return GetContext( cNew.Context ).Register( cNew, c );
        }

        public MultiAmbiantContractResult GetResult()
        {
            // We filter duplicates with the help of a flag (instead of an HashSet).
            // Each call to this GetResult() reverts the "non processed" flag.
            _processedResultFlag = !_processedResultFlag;

            var r = new MultiAmbiantContractResult();
            r.Add( _defaultCtx.GetResult( _processedResultFlag ) );
            foreach( PreContext ctx in _ctx.Values )
            {
                r.Add( ctx.GetResult( _processedResultFlag ) );
            }
            return r;
        }

    }

}
