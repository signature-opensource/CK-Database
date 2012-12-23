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
    public class AmbientContextAttributesCache : ICustomAttributeTypeProvider
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


        /// <summary>
        /// Exposes the Type that is managed by this object to specializations.
        /// </summary>
        protected readonly Type Type;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextAttributesCache"/>.
        /// </summary>
        /// <param name="t"></param>
        public AmbientContextAttributesCache( Type t )
        {
            if( t == null ) throw new ArgumentNullException( "t" );
            Type = t;
            var all = new List<Entry>();
            Register( all, t );
            foreach( var m in t.GetMembers( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                Register( all, m );
            }
            _all = all.ToArray();
        }

        static void Register( List<Entry> all, MemberInfo m )
        {
            var attr = (IAttributeAmbientContextBound[])m.GetCustomAttributes( typeof( IAttributeAmbientContextBound ), false );
            foreach( var a in attr )
            {
                a.Initialize( m );
                all.Add( new Entry( m, a ) );
            }
        }

        /// <summary>
        /// Gets attributes on the <see cref="P:Type"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes( Type attributeType )
        {
            return DoGetCustomAttributes( Type, attributeType );
        }

        /// <summary>
        /// Gets attributes on a <see cref="MethodInfo"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <param name="m">Method of <see cref="P:Type"/>.</param>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes( MethodInfo m, Type attributeType )
        {
            return DoGetCustomAttributes( m, attributeType );
        }

        /// <summary>
        /// Gets attributes on a <see cref="PropertyInfo"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <param name="p">Property of <see cref="P:Type"/>.</param>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes<T>( PropertyInfo p, Type attributeType )
        {
            return DoGetCustomAttributes( p, attributeType );
        }

        /// <summary>
        /// Gets attributes on the <see cref="P:Type"/> that are assignable to <typeparamref name="T"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <typeparam name="T">Type that must be supported by the attributes.</typeparam>
        /// <returns>A set of attributes that are guaranteed to be assignable to <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetCustomAttributes<T>()
        {
            return DoGetCustomAttributes<T>( Type );
        }

        /// <summary>
        /// Gets attributes on a <see cref="MethodInfo"/> that are assignable to <typeparamref name="T"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <typeparam name="T">Type that must be supported by the attributes.</typeparam>
        /// <param name="m">Method of <see cref="P:Type"/>.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetCustomAttributes<T>( MethodInfo m )
        {
            return DoGetCustomAttributes<T>( m );
        }

        /// <summary>
        /// Gets attributes on a <see cref="PropertyInfo"/> that are assignable to <typeparamref name="T"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <typeparam name="T">Type that must be supported by the attributes.</typeparam>
        /// <param name="p">Property of <see cref="P:Type"/>.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <typeparamref name="T"/>.</returns>
        public IEnumerable<T> GetCustomAttributes<T>( PropertyInfo p )
        {
            return DoGetCustomAttributes<T>( p );
        }

        IEnumerable<object> ICustomAttributeProvider.GetCustomAttributes( MemberInfo m, Type attributeType )
        {
            return DoGetCustomAttributes( m, attributeType );
        }

        private IEnumerable<object> DoGetCustomAttributes( MemberInfo m, Type attributeType )
        {
            Guard( m );
            if( attributeType == null ) throw new ArgumentNullException( "attributeType" );
            return _all.Where( e => e.M == m && attributeType.IsAssignableFrom( e.Attr.GetType() ) ).Select( e => e.Attr )
                    .Concat( m.GetCustomAttributes( attributeType, false ).Where( a => !(a is IAttributeAmbientContextBound) ) );
        }

        void Guard( MemberInfo m )
        {
            if( m == null ) throw new ArgumentNullException( "m" );
            if( m != Type && !m.DeclaringType.IsAssignableFrom( Type ) ) throw new CKException( "Member {0}.{1} does not belong to {2}.", m.DeclaringType.FullName, m.Name, Type.FullName );
        }

        IEnumerable<T> ICustomAttributeProvider.GetCustomAttributes<T>( MemberInfo m )
        {
            return DoGetCustomAttributes<T>( m );
        }

        private IEnumerable<T> DoGetCustomAttributes<T>( MemberInfo m )
        {
            Guard( m );
            return _all.Where( e => e.M == m && e.Attr is T ).Select( e => (T)e.Attr )
                    .Concat( m.GetCustomAttributes( typeof( T ), false ).Where( a => !(a is IAttributeAmbientContextBound) ).Select( a => (T)a ) );
        }


    }

}
