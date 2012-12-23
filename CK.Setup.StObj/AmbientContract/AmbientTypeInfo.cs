using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CK.Core
{

    /// <summary>
    /// Encapsulate type information for an Ambient Contract class and offers a <see cref="FinalContexts"/> collection that 
    /// exposes the different contexts that contain the type.
    /// It is a concrete class that can be specialized to capture more specific information related to the type: 
    /// the virtual <see cref="CreateContextTypeInfo"/> factory method should be overrriden to create 
    /// appropriate <see cref="AmbientContextTypeInfo{T}"/> contextualized type information.
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


        /// <summary>
        /// Used only for Empty Object Pattern implementations.
        /// </summary>
        protected AmbientTypeInfo()
        {
            Type = typeof( object );
        }

        public AmbientTypeInfo Generalization { get; private set; }

        public IReadOnlyCollection<string> FinalContexts
        {
            get { return _finalContextsEx; }
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

        internal bool CollectDeepestConcrete<T,TC>( IActivityLogger logger, DynamicAssembly assembly, List<TC> lastConcretes, List<Type> abstractTails, string context )
            where T : AmbientTypeInfo
            where TC : AmbientContextTypeInfo<T>
        {
            Debug.Assert( context != null );
            bool concreteBelow = false;
            AmbientTypeInfo c = _firstChild;
            while( c != null )
            {
                if( c.MutableFinalContexts.Contains( context ) )
                {
                    concreteBelow |= c.CollectDeepestConcrete<T,TC>( logger, assembly, lastConcretes, abstractTails, context );
                }
                c = c._nextSibling;
            }
            if( !concreteBelow )
            {
                var ct = CreateContextTypeInfo<T,TC>( context, null );
                if( Type.IsAbstract && (assembly == null || !ct.AbstractTypeCanBeInstanciated( logger, assembly )) )
                {
                    abstractTails.Add( Type );
                }
                else
                {
                    lastConcretes.Add( ct );
                    concreteBelow = true;
                }
            }
            return concreteBelow;
        }

        /// <summary>
        /// Factory method for associated contextualized type.
        /// </summary>
        /// <typeparam name="T">This specialized AmbientTypeInfo.</typeparam>
        /// <typeparam name="TC">Type of associated contextualized specialization.</typeparam>
        /// <param name="context">Context name for which the associated contextualized specialization must be instanciated.</param>
        /// <param name="specialization">Specialization if any.</param>
        /// <returns>Associated contextualized type information.</returns>
        internal virtual protected TC CreateContextTypeInfo<T,TC>( string context, TC specialization )
            where T : AmbientTypeInfo
            where TC : AmbientContextTypeInfo<T>
        {
            return (TC)new AmbientContextTypeInfo<T>( (T)this, context, specialization );
        }

        static void ProcessContextAttributes<T>( Type t, Func<string, bool> action ) where T : IAttributeContext
        {
            object[] attrs = t.GetCustomAttributes( typeof( T ), false );
            foreach( var a in attrs ) action( ((IAttributeContext)a).Context );
        }
    }
}
