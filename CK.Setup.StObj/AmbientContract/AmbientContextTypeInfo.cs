using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Caches attributes that support <see cref="IAttributeAmbientContextBound"/> interface.
    /// </summary>
    public class AmbientContextTypeInfo<TAmbientTypeInfo> where TAmbientTypeInfo : AmbientTypeInfo
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

        Entry[] _all;

        /// <summary>
        /// Initializes a new <see cref="AmbientContextTypeInfo"/>. 
        /// Attributes must be retrieved with <see cref="GetCustomAttributes"/> methods.
        /// </summary>
        /// <param name="t">Type for which <see cref="IAttributeAmbientContextBound"/> attributes must be cached.</param>
        /// <param name="context">Context name.</param>
        internal AmbientContextTypeInfo( TAmbientTypeInfo t, string context )
        {
            Debug.Assert( t != null && context != null );
            AmbientTypeInfo = t;
            Context = context;
            var all = new List<Entry>();
            Register( all, t.Type );
            foreach( var m in t.Type.GetMembers( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ) )
            {
                Register( all, m );
            }
            _all = all.ToArray();
        }

        static void Register( List<Entry> all, MemberInfo m )
        {
            var t2 = (IAttributeAmbientContextBound[])m.GetCustomAttributes( typeof( IAttributeAmbientContextBound ), false );
            foreach( var a in t2 )
            {
                a.Initialize( m );
                all.Add( new Entry( m, a ) );
            }
        }

        /// <summary>
        /// Ambient type information.
        /// </summary>
        public readonly TAmbientTypeInfo AmbientTypeInfo;

        /// <summary>
        /// Context name of this contextual information.
        /// </summary>
        public readonly string Context;

        /// <summary>
        /// Gets attributes on the <see cref="Type"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes( Type attributeType )
        {
            return DoGetCustomAttributes( AmbientTypeInfo.Type, attributeType );
        }

        /// <summary>
        /// Gets attributes on a <see cref="MethodInfo"/> that are assignable to <paramref name="attributeType"/>.
        /// Instances of attributes that support <see cref="IAttributeAmbientContextBound"/> are always the same. 
        /// Other attributes are instanciated (by calling <see cref="MemberInfo.GetCustomAttributes"/>).
        /// </summary>
        /// <param name="m">Method of <see cref="Type"/>.</param>
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
        /// <param name="p">Property of <see cref="Type"/>.</param>
        /// <param name="attributeType">Type that must be supported by the attributes.</param>
        /// <returns>A set of attributes that are guaranteed to be assignable to <paramref name="attributeType"/>.</returns>
        public IEnumerable<object> GetCustomAttributes( PropertyInfo p, Type attributeType )
        {
            return DoGetCustomAttributes( p, attributeType );
        }

        IEnumerable<object> DoGetCustomAttributes( MemberInfo m, Type attributeType )
        {
            return _all.Where( e => e.M == m && attributeType.IsAssignableFrom( e.Attr.GetType() ) ).Select( e => e.Attr )
                    .Concat( m.GetCustomAttributes( attributeType, false ).Where( a => !(a is IAttributeAmbientContextBound) ) );
        }
    }

}
