using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    public class AmbientTypeInfo
    {
        public readonly Type Type;

        internal readonly ISet<Type> MutableFinalContexts;
        readonly IReadOnlyCollection<Type> _finalContextsEx;

        readonly AmbientTypeInfo _nextSibling;
        AmbientTypeInfo _firstChild;
        Type[] _ambientInterfaces;
        Type[] _thisAmbientInterfaces;

        public AmbientTypeInfo( AmbientTypeInfo parent, Type t )
        {
            Type = t;
            MutableFinalContexts = new HashSet<Type>();
            _finalContextsEx = new ReadOnlyCollectionOnISet<Type>( MutableFinalContexts );
            if( (DirectGeneralization = parent) == null )
            {
                _nextSibling = null;
                MutableFinalContexts.Add( AmbientContractCollector.DefaultContext );
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

        public AmbientTypeInfo DirectGeneralization { get; private set; }

        public IReadOnlyCollection<Type> FinalContexts
        {
            get { return _finalContextsEx; }
        }

        Type[] EnsureAllAmbientInterfaces( Func<Type,bool> ambientInterfacePredicate )
        {
            return _ambientInterfaces ?? (_ambientInterfaces = Type.GetInterfaces().Where( ambientInterfacePredicate ).ToArray());
        }

        internal Type[] EnsureThisAmbientInterfaces( Func<Type, bool> ambientInterfacePredicate )
        {
            return _thisAmbientInterfaces ?? (_thisAmbientInterfaces = DirectGeneralization != null 
                                                        ? EnsureAllAmbientInterfaces( ambientInterfacePredicate ).Except( DirectGeneralization.EnsureAllAmbientInterfaces( ambientInterfacePredicate ) ).ToArray() 
                                                        : EnsureAllAmbientInterfaces( ambientInterfacePredicate ));
        }

        public IEnumerable<AmbientTypeInfo> SpecializationsByContext( Type context )
        {
            AmbientTypeInfo c = _firstChild;
            while( c != null )
            {
                if( context == null || c.MutableFinalContexts.Contains( context ) ) yield return c;
                c = c._nextSibling;
            }
        }

        internal bool CollectDeepestConcrete( List<AmbientTypeInfo> lastConcretes, List<Type> abstractTails, Type context = null )
        {
            bool concreteBelow = false;
            AmbientTypeInfo c = _firstChild;
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

        internal List<TAmbientTypeInfo> FillPath<TAmbientTypeInfo>( List<TAmbientTypeInfo> path ) where TAmbientTypeInfo : AmbientTypeInfo
        {
            if( DirectGeneralization != null ) DirectGeneralization.FillPath( path );
            path.Add( (TAmbientTypeInfo)this );
            return path;
        }
    }
}
