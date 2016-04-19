#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.StObj.Engine\AmbientContract\AmbientContextualAttributesCache.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Implements a cache for attributes associated to a type or to any of its members that support <see cref="IAttributeAmbientContextBound"/>.
    /// Attribute inheritance is ignored: only attributes applied to the member are considered. 
    /// When used with another type or a member of another type from the one provided 
    /// in the constructor, an exception is thrown.
    /// </summary>
    public class AmbientContextualAttributesCache : ICKCustomAttributeTypeMultiProvider
    {
        struct Entry
        {
            public Entry( MemberInfo m, object a )
            {
                M = m;
                Attr = a;
            }

            public readonly MemberInfo M;
            public readonly object Attr;
        }
        readonly Entry[] _all;
        readonly MemberInfo[] _typeMembers;
        readonly bool _includeBaseClasses;
        readonly Type _type;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextualAttributesCache"/> that considers only members explicitely 
        /// declared by the <paramref name="type"/>.
        /// </summary>
        /// <param name="type">Type for which attributes must be cached.</param>
        /// <param name="includeBaseClasses">True to include attributes of base classes and attributes on members of the base classes.</param>
        public AmbientContextualAttributesCache( Type type, bool includeBaseClasses )
        {
            if( type == null ) throw new ArgumentNullException( "t" );
            _type = type;
            var all = new List<Entry>();
            int initializerCount = Register( all, type, includeBaseClasses );
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            if( includeBaseClasses ) flags &= ~BindingFlags.DeclaredOnly;
            _typeMembers = type.GetMembers( flags );
            foreach( var m in _typeMembers ) initializerCount += Register( all, m );
            _all = all.ToArray();
            _includeBaseClasses = includeBaseClasses;
            if( initializerCount > 0 )
            {
                foreach( Entry e in _all )
                {
                    IAttributeAmbientContextBoundInitializer aM = e.Attr as IAttributeAmbientContextBoundInitializer;
                    if( aM != null )
                    {
                        aM.Initialize( this, e.M );
                        if( --initializerCount == 0 ) break;
                    }
                }
            }
        }

        static int Register( List<Entry> all, MemberInfo m, bool inherit = false )
        {
            int initializerCount = 0;
            var attr = (IAttributeAmbientContextBound[])m.GetCustomAttributes( typeof( IAttributeAmbientContextBound ), inherit );
            foreach( var a in attr )
            {
                AmbientContextBoundDelegationAttribute delegated = a as AmbientContextBoundDelegationAttribute;
                object finalAttributeToUse = a;
                if( delegated != null )
                {
                    Type dT = SimpleTypeFinder.WeakResolver( delegated.ActualAttributeTypeAssemblyQualifiedName, true );
                    finalAttributeToUse = Activator.CreateInstance( dT, new object[] { a } );
                }
                all.Add( new Entry( m, finalAttributeToUse ) );
                if( finalAttributeToUse is IAttributeAmbientContextBoundInitializer ) ++initializerCount;
            }
            return initializerCount;
        }

        /// <summary>
        /// Get the Type that is managed by this cache for specialized classes.
        /// They can use another name than 'Type' to expose it if they will.
        /// </summary>
        protected Type Type { get { return _type; } }

        /// <summary>
        /// The Type property of the ICustomAttributeTypeMultiProvider is hidden here to enable specialized classes
        /// to expose it with a different name.
        /// </summary>
        Type ICKCustomAttributeTypeMultiProvider.Type { get { return _type; } }

        /// <summary>
        /// Gets whether an attribute that is assignable to the given <paramref name="attributeType"/> 
        /// exists on the given member.
        /// </summary>
        /// <param name="m">The member.</param>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <returns>True if at least one attribute exists.</returns>
        public bool IsDefined( MemberInfo m, Type attributeType )
        {
            if( m == null ) throw new ArgumentNullException( "m" );
            if( attributeType == null ) throw new ArgumentNullException( "attributeType" );
            return _all.Any( e => CK.Reflection.MemberInfoEqualityComparer.Default.Equals( e.M, m ) && attributeType.IsAssignableFrom( e.Attr.GetType() ) )
                    || ( (m.DeclaringType == Type || (_includeBaseClasses && m.DeclaringType.IsAssignableFrom( Type ))) && m.IsDefined( attributeType, false ) );
        }

        /// <summary>
        /// Gets attributes on a <see cref="MemberInfo"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes(Type,bool)"/>).
        /// </summary>
        /// <param name="m">Method of <see cref="P:Type"/>.</param>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes( MemberInfo m, Type attributeType )
        {
            if( m == null ) throw new ArgumentNullException( "m" );
            if( attributeType == null ) throw new ArgumentNullException( "attributeType" );
            var fromCache = _all.Where( e => CK.Reflection.MemberInfoEqualityComparer.Default.Equals( e.M, m ) && attributeType.IsAssignableFrom( e.Attr.GetType() ) ).Select( e => e.Attr );
            if( m.DeclaringType == Type || (_includeBaseClasses && m.DeclaringType.IsAssignableFrom( Type )) )
            {
                return fromCache
                        .Concat( m.GetCustomAttributes( attributeType, false ).Where( a => !(a is IAttributeAmbientContextBound) ) );
            }
            return fromCache;
        }

        /// <summary>
        /// Gets attributes on a <see cref="MemberInfo"/> that are assignable to <typeparamref name="T"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes(Type,bool)"/>).
        /// </summary>
        /// <typeparam name="T">Type that must be supported by the attributes.</typeparam>
        /// <param name="m">Method of <see cref="P:Type"/>.</param>
        /// <returns>A set of typed attributes.</returns>
        public IEnumerable<T> GetCustomAttributes<T>( MemberInfo m )
        {
            if( m == null ) throw new ArgumentNullException( "m" );
            var fromCache = _all.Where( e => CK.Reflection.MemberInfoEqualityComparer.Default.Equals( e.M, m ) && e.Attr is T ).Select( e => (T)e.Attr );
            if( m.DeclaringType == Type || (_includeBaseClasses && m.DeclaringType.IsAssignableFrom( Type )) )
            {
                return fromCache
                        .Concat( m.GetCustomAttributes( typeof( T ), false ).Where( a => !(a is IAttributeAmbientContextBound) ).Select( a => (T)a ) );
            }
            return fromCache;
        }

        IEnumerable<object> ICKCustomAttributeMultiProvider.GetAllCustomAttributes( Type attributeType )
        {
            return GetAllCustomAttributes( attributeType );
        }

        /// <summary>
        /// Gets all attributes that are assignable to the given <paramref name="attributeType"/>, regardless of the <see cref="MemberInfo"/>
        /// that carries it. 
        /// </summary>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <param name="memberOnly">True to ignore attributes of the type itself.</param>
        /// <returns>Enumeration of attributes (possibly empty).</returns>
        public IEnumerable<object> GetAllCustomAttributes( Type attributeType, bool memberOnly = false )
        {
            var fromCache = _all.Where( e => (!memberOnly || e.M != Type) && attributeType.IsAssignableFrom( e.Attr.GetType() ) ).Select( e => e.Attr );
            var fromMembers = _typeMembers.SelectMany( m => m.GetCustomAttributes( attributeType, false ).Where( a => !(a is IAttributeAmbientContextBound) ) );
            if( memberOnly ) return fromCache.Concat( fromMembers );
            var fromType = Type.GetCustomAttributes( attributeType, _includeBaseClasses ).Where( a => !(a is IAttributeAmbientContextBound) );
            return fromCache.Concat( fromType ).Concat( fromMembers );
        }

        IEnumerable<T> ICKCustomAttributeMultiProvider.GetAllCustomAttributes<T>()
        {
            return GetAllCustomAttributes<T>();
        }

        /// <summary>
        /// Gets all attributes that are assignable to the given type, regardless of the <see cref="MemberInfo"/>
        /// that carries it.
        /// </summary>
        /// <typeparam name="T">Type of the attributes.</typeparam>
        /// <param name="memberOnly">True to ignore attributes of the type itself.</param>
        /// <returns>Enumeration of attributes (possibly empty).</returns>
        public IEnumerable<T> GetAllCustomAttributes<T>( bool memberOnly = false )
        {
            var fromCache = _all.Where( e => e.Attr is T && (!memberOnly || e.M != Type) ).Select( e => (T)e.Attr );
            var fromMembers = _typeMembers.SelectMany( m => m.GetCustomAttributes( typeof( T ), false )
                                            .Where( a => !(a is IAttributeAmbientContextBound) ) )
                                            .Select( a => (T)a );
            if( memberOnly ) return fromCache.Concat( fromMembers );
            var fromType = Type.GetCustomAttributes( typeof( T ), _includeBaseClasses ).Where( a => !(a is IAttributeAmbientContextBound) ).Select( a => (T)a );
            return fromCache.Concat( fromType ).Concat( fromMembers );
        }

        /// <summary>
        /// Gets all <see cref="MemberInfo"/> that this <see cref="ICKCustomAttributeMultiProvider"/> handles.
        /// The <see cref="Type"/> is appended to this list.
        /// </summary>
        /// <returns>Enumeration of members.</returns>
        public IEnumerable<MemberInfo> GetMembers()
        {
            return _typeMembers.Append( Type );
        }

    }

}
