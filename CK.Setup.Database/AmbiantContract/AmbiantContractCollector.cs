using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbiantContractCollector
    {
        Dictionary<Type,ClassType> _classMap;
        int _regTypeCount;
        List<ClassType> _classMapAmbiguities;

        class ClassType : List<Type>
        {
            public ClassType( Type final )
            {
                for( ; ; )
                {
                    Add( final );
                    final = final.BaseType;
                    if( final == typeof( object ) || !typeof( IAmbiantContract ).IsAssignableFrom( final ) ) break;
                }
                Reverse();
                IndexOfFinalConcrete = Count - 1;
                while( IndexOfFinalConcrete >= 0 && this[IndexOfFinalConcrete].IsAbstract ) --IndexOfFinalConcrete;
            }

            public Type Head { get { return this[0]; } }
            public Type Final { get { return this[Count - 1]; } }
            public readonly int IndexOfFinalConcrete;
            public bool HasFinalConcrete { get { return IndexOfFinalConcrete >= 0; } }
            public bool HasAbstractTail { get { return IndexOfFinalConcrete < Count - 1; } }
            public Type FinalConcrete { get { return this[IndexOfFinalConcrete]; } }
            public IEnumerable<Type> ToFinalConcrete { get { return this.Take( IndexOfFinalConcrete + 1 ); } }
            public IEnumerable<Type> AbstractTail { get { return this.Skip( IndexOfFinalConcrete ); } }
        }

        public AmbiantContractCollector()
        {
            _classMap = new Dictionary<Type, ClassType>();
        }

        public int RegisteredTypeCount
        {
            get { return _regTypeCount; }
        }

        public void Register( IEnumerable<Type> types, Action<Type> onClassRegistered )
        {
            foreach( var t in types.Where( c => c.IsClass
                                                && typeof( IAmbiantContract ).IsAssignableFrom( c )
                                                && !_classMap.ContainsKey( c ) ) )
            {
                if( DoReg( t ) ) onClassRegistered( t );
            }
        }

        public bool RegisterClass( Type c )
        {
            if( c == null ) throw new ArgumentNullException();
            if( !c.IsClass ) throw new ArgumentException();
            if( typeof( IAmbiantContract ).IsAssignableFrom( c ) && !_classMap.ContainsKey( c ) )
            {
                return DoReg( c );
            }
            return false;
        }

        bool DoReg( Type c )
        {
            int deltaNew;
            ClassType cNew = new ClassType( c );
            ClassType cPrv;
            if( _classMap.TryGetValue( cNew.Head, out cPrv ) )
            {
                deltaNew = cNew.Count - cPrv.Count;
                // If the new path is smaller than the existing one
                // or if the existing one is not a prefix for the new one, 
                // this is an ambiguity. 
                // Else, the new one extends the previous one: we replace it.
                if( deltaNew <= 0 || cPrv.Final != cNew[cPrv.Count - 1] )
                {
                    if( _classMapAmbiguities == null ) _classMapAmbiguities = new List<ClassType>();
                    _classMapAmbiguities.Add( cNew );
                    return false;
                }
            }
            else deltaNew = cNew.Count;

            foreach( var t in cNew ) _classMap[t] = cNew;
            _regTypeCount += deltaNew;

            return true;
        }

        public AmbiantContractCollectorResult GetResult()
        {
            Dictionary<Type,Type> mappings = new Dictionary<Type, Type>();
            List<Type> concreteClasses = new List<Type>();
            List<Tuple<Type,Type>> itfA = null;
            List<IReadOnlyList<Type>> abstractClasses = null;
            List<IReadOnlyList<Type>> abstractTails = null;

            foreach( ClassType ct in _classMap.Values )
            {
                if( ct.HasFinalConcrete )
                {
                    Type fc = ct.FinalConcrete;
                    concreteClasses.Add( fc );
                    foreach( Type mapped in ct.ToFinalConcrete ) mappings.Add( mapped, fc );
                    foreach( Type iFace in fc.GetInterfaces() )
                    {
                        if( typeof( IAmbiantContract ).IsAssignableFrom( iFace ) && iFace != typeof( IAmbiantContract ) )
                        {
                            Type alreadyMapped = mappings.GetValueWithDefault( iFace, null );
                            if( alreadyMapped != null )
                            {
                                if( itfA == null ) itfA = new List<Tuple<Type, Type>>();
                                itfA.Add( Tuple.Create( iFace, alreadyMapped ) );
                            }
                            else
                            {
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

            return new AmbiantContractCollectorResult( mappings, concreteClasses.ToReadOnlyList(), clsA, itfA, abstractClasses, abstractTails );
        }

    }

}
