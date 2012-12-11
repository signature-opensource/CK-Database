using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Encapsulate type information for an Ambient Contract class and offers a <see cref="FinalContexts"/> collection that 
    /// exposes the different contexts that contain the type.
    /// It is a concrete class that can be specialized to capture more specific information related to the type.
    /// </summary>
    public class AmbientTypeInfo
    {
        public readonly Type Type;

        internal readonly ISet<string> MutableFinalContexts;
        readonly IReadOnlyCollection<string> _finalContextsEx;

        readonly AmbientTypeInfo _nextSibling;
        AmbientTypeInfo _firstChild;
        Type[] _ambientInterfaces;
        Type[] _thisAmbientInterfaces;

        /// <summary>
        /// Initializes a new <see cref="AmbientTypeInfo"/> from a base one (its <see cref="Generalization"/>) if it exists and a type.
        /// </summary>
        /// <param name="parent">Parent AmbientTypeInfo. Null if the base type is not an Ambient Contract.</param>
        /// <param name="t">Type itself. Can not be null.</param>
        public AmbientTypeInfo( AmbientTypeInfo parent, Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            Type = t;
            MutableFinalContexts = new HashSet<string>();
            _finalContextsEx = new ReadOnlyCollectionOnISet<string>( MutableFinalContexts );
            if( (Generalization = parent) == null )
            {
                _nextSibling = null;
                MutableFinalContexts.Add( String.Empty );
            }
            else
            {
                MutableFinalContexts.AddRange( Generalization.MutableFinalContexts );
                _nextSibling = Generalization._firstChild;
                Generalization._firstChild = this;
            }
            ProcessContextAttributes<AddContextAttribute>( t, MutableFinalContexts.Add );
            ProcessContextAttributes<RemoveContextAttribute>( t, MutableFinalContexts.Remove );
        }

        public AmbientTypeInfo Generalization { get; private set; }

        public IReadOnlyCollection<string> FinalContexts
        {
            get { return _finalContextsEx; }
        }

        /// <summary>
        /// Gets whether this <see cref="Type"/> (that is abstract) must actually be considered as an abstract type or not.
        /// An abstract class may be considered as concrete if there is a way to concretize an instance. 
        /// This method can prepare.
        /// </summary>
        protected virtual bool AbstractTypeCanBeInstanciated()
        {
            Debug.Assert( Type.IsAbstract );
            return false;
        }


        Type[] EnsureAllAmbientInterfaces( Func<Type,bool> ambientInterfacePredicate )
        {
            return _ambientInterfaces ?? (_ambientInterfaces = Type.GetInterfaces().Where( ambientInterfacePredicate ).ToArray());
        }

        internal Type[] EnsureThisAmbientInterfaces( Func<Type, bool> ambientInterfacePredicate )
        {
            return _thisAmbientInterfaces ?? (_thisAmbientInterfaces = Generalization != null 
                                                        ? EnsureAllAmbientInterfaces( ambientInterfacePredicate ).Except( Generalization.EnsureAllAmbientInterfaces( ambientInterfacePredicate ) ).ToArray() 
                                                        : EnsureAllAmbientInterfaces( ambientInterfacePredicate ));
        }

        /// <summary>
        /// Gets the different specialized <see cref="AmbientTypeInfo"/> that exist in a given context or in all context (when <paramref name="context"/> is null).
        /// </summary>
        /// <param name="context">Named context. Null to get all specializations regardless of their context.</param>
        /// <returns>An enumerable of <see cref="AmbientTypeInfo"/> that specialize this one.</returns>
        public IEnumerable<AmbientTypeInfo> SpecializationsByContext( string context )
        {
            AmbientTypeInfo c = _firstChild;
            while( c != null )
            {
                if( context == null || c.MutableFinalContexts.Contains( context ) ) yield return c;
                c = c._nextSibling;
            }
        }

        internal bool CollectDeepestConcrete( List<AmbientTypeInfo> lastConcretes, List<Type> abstractTails, string context = null )
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
                if( Type.IsAbstract && !AbstractTypeCanBeInstanciated() )
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

        static void ProcessContextAttributes<T>( Type t, Func<string, bool> action ) where T : IContextDefiner
        {
            object[] attrs = t.GetCustomAttributes( typeof( T ), false );
            foreach( var a in attrs ) action( ((IContextDefiner)a).Context );
        }

        internal List<TAmbientTypeInfo> FillPath<TAmbientTypeInfo>( List<TAmbientTypeInfo> path ) where TAmbientTypeInfo : AmbientTypeInfo
        {
            if( Generalization != null ) Generalization.FillPath( path );
            path.Add( (TAmbientTypeInfo)this );
            return path;
        }
    }
}
