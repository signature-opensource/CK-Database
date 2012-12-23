using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Offers a way to retrieve attributes on <see cref="MemberInfo"/>.
    /// This is a basic interface that is not bound to a type or an assembly or to any special holders.
    /// Its goal is to support a way to inject behavior related to attributes such as caching them for instance in 
    /// order to enable attributes to share state between different aspects (but inside the same process or context).
    /// </summary>
    public interface ICustomAttributeProvider
    {
        /// <summary>
        /// Gets a set of attributes that are assignable to the given <see cref="attributeType"/>.
        /// </summary>
        /// <param name="m">The member info (can be a <see cref="Type"/>).</param>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <returns>Enumeration possibly empty.</returns>
        IEnumerable<object> GetCustomAttributes( MemberInfo m, Type attributeType );

        /// <summary>
        /// Strongly typed version.
        /// </summary>
        /// <typeparam name="T">Type of the attributes.</typeparam>
        /// <param name="m">The member info (can be a <see cref="Type"/>).</param>
        /// <returns>Enumeration possibly empty.</returns>
        IEnumerable<T> GetCustomAttributes<T>( MemberInfo m );
    }
}
