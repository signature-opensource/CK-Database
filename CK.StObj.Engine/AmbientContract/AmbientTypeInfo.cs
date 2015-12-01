#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\AmbientTypeInfo.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
    /// appropriate AmbientContextualTypeInfo&lt;T&gt; contextualized type information.
    /// </summary>
    public class AmbientTypeInfo
    {
        /// <summary>
        /// Type that this instance decorates.
        /// </summary>
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
            _finalContextsEx = new CKReadOnlyCollectionOnISet<string>( MutableFinalContexts );
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
        /// Used only for Empty Item Pattern implementations.
        /// </summary>
        protected AmbientTypeInfo()
        {
            Type = typeof( object );
        }

        /// <summary>
        /// Gets the generalizatiuon of this <see cref="Type"/>.
        /// </summary>
        public AmbientTypeInfo Generalization { get; private set; }

        /// <summary>
        /// Gets the contexts to which this <see cref="Type"/> appears.
        /// </summary>
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

        internal bool CollectDeepestConcrete<T, TC>( IActivityMonitor monitor, IContextualTypeMap context, TC generalization, DynamicAssembly assembly, List<Tuple<TC,object>> lastConcretes, List<Type> abstractTails )
            where T : AmbientTypeInfo
            where TC : AmbientContextualTypeInfo<T,TC>
        {
            // Creates the TC associated to T in context here: it may be unused but this is required
            // in order for AbstractTypeCanBeInstanciated to be able to use Generalization information (attributes cache).
            var ct = CreateContextTypeInfo<T, TC>( generalization, context );
            Debug.Assert( context != null );
            bool concreteBelow = false;
            AmbientTypeInfo c = _firstChild;
            while( c != null )
            {
                if( c.MutableFinalContexts.Contains( context.Context ) )
                {
                    concreteBelow |= c.CollectDeepestConcrete<T, TC>( monitor, context, ct, assembly, lastConcretes, abstractTails );
                }
                c = c._nextSibling;
            }
            if( !concreteBelow )
            {
                object abstractTypeInfo = null;
                if( Type.IsAbstract && (assembly == null || !ct.AbstractTypeCanBeInstanciated( monitor, assembly, out abstractTypeInfo )) )
                {
                    abstractTails.Add( Type );
                }
                else
                {
                    lastConcretes.Add( Tuple.Create( ct, abstractTypeInfo ) );
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
        /// <param name="generalization">Generalization if any (null for root of Types path).</param>
        /// <returns>Associated contextualized type information.</returns>
        internal virtual protected TC CreateContextTypeInfo<T, TC>( TC generalization, IContextualTypeMap context )
            where T : AmbientTypeInfo
            where TC : AmbientContextualTypeInfo<T, TC>
        {
            return (TC)new AmbientContextualTypeInfo<T,TC>( (T)this, generalization, context );
        }

        static void ProcessContextAttributes<T>( Type t, Func<string, bool> action ) where T : IAddOrRemoveContextAttribute
        {
            object[] attrs = t.GetCustomAttributes( typeof( T ), false );
            foreach( var a in attrs ) action( ((IAddOrRemoveContextAttribute)a).Context );
        }
    }
}
