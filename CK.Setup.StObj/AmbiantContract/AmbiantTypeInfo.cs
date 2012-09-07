using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbiantTypeInfo
    {
        public readonly Type Type;

        internal readonly ISet<Type> MutableFinalContexts;
        readonly IReadOnlyCollection<Type> _finalContextsEx;

        readonly AmbiantTypeInfo _nextSibling;
        AmbiantTypeInfo _firstChild;
        Type[] _ambiantInterfaces;
        Type[] _thisAmbiantInterfaces;

        public AmbiantTypeInfo( AmbiantTypeInfo parent, Type t )
        {
            Type = t;
            MutableFinalContexts = new HashSet<Type>();
            _finalContextsEx = new ReadOnlyCollectionOnISet<Type>( MutableFinalContexts );
            if( (DirectGeneralization = parent) == null )
            {
                _nextSibling = null;
                MutableFinalContexts.Add( AmbiantContractCollector.DefaultContext );
            }
            else
            {
                MutableFinalContexts.AddRange( DirectGeneralization.MutableFinalContexts );
                _nextSibling = DirectGeneralization._firstChild;
                DirectGeneralization._firstChild = this;
            }
            ProcessContextAttributes<AddContextAttribute>( t, MutableFinalContexts.Add );
            ProcessContextAttributes<RemoveContextAttribute>( t, MutableFinalContexts.Remove );
        }

        public AmbiantTypeInfo DirectGeneralization { get; private set; }

        public IReadOnlyCollection<Type> FinalContexts
        {
            get { return _finalContextsEx; }
        }

        Type[] EnsureAllAmbiantInterfaces( Func<Type,bool> ambiantInterfacePredicate )
        {
            return _ambiantInterfaces ?? (_ambiantInterfaces = Type.GetInterfaces().Where( ambiantInterfacePredicate ).ToArray());
        }

        internal Type[] EnsureThisAmbiantInterfaces( Func<Type, bool> ambiantInterfacePredicate )
        {
            return _thisAmbiantInterfaces ?? (_thisAmbiantInterfaces = DirectGeneralization != null 
                                                        ? EnsureAllAmbiantInterfaces( ambiantInterfacePredicate ).Except( DirectGeneralization.EnsureAllAmbiantInterfaces( ambiantInterfacePredicate ) ).ToArray() 
                                                        : EnsureAllAmbiantInterfaces( ambiantInterfacePredicate ));
        }

        public IEnumerable<AmbiantTypeInfo> SpecializationsByContext( Type context )
        {
            AmbiantTypeInfo c = _firstChild;
            while( c != null )
            {
                if( context == null || c.MutableFinalContexts.Contains( context ) ) yield return c;
                c = c._nextSibling;
            }
        }

        internal bool CollectDeepestConcrete( List<AmbiantTypeInfo> lastConcretes, List<Type> abstractTails, Type context = null )
        {
            bool concreteBelow = false;
            AmbiantTypeInfo c = _firstChild;
            while( c != null )
            {
                if( context == null || c.MutableFinalContexts.Contains( context ) )
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

        internal List<TAmbiantTypeInfo> FillPath<TAmbiantTypeInfo>( List<TAmbiantTypeInfo> path ) where TAmbiantTypeInfo : AmbiantTypeInfo
        {
            if( DirectGeneralization != null ) DirectGeneralization.FillPath( path );
            path.Add( (TAmbiantTypeInfo)this );
            return path;
        }
    }
}
