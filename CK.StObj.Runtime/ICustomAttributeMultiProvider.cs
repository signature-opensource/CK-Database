using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="ICustomAttributeProvider"/> to handle multiple <see cref="MemberInfo"/> (typically bound to a <see cref="Type"/>
    /// and all its members).
    /// This interface actually applies to any set of attribute holders (it may be implemented for a whole assembly for instance).
    /// </summary>
    public interface ICustomAttributeMultiProvider : ICustomAttributeProvider
    {
        /// <summary>
        /// Gets all attributes that are assignable to the given type, regardless of the <see cref="MemberInfo"/>
        /// that carries it.
        /// </summary>
        /// <typeparam name="T">Type of the attributes.</typeparam>
        /// <returns>Enumeration of attributes (possibly empty).</returns>
        IEnumerable<T> GetAllCustomAttributes<T>();

        /// <summary>
        /// Gets all attributes that are assignable to the given <paramref name="attributeType"/>, regardless of the <see cref="MemberInfo"/>
        /// that carries it. 
        /// </summary>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <returns>Enumeration of attributes (possibly empty).</returns>
        IEnumerable<object> GetAllCustomAttributes( Type attributeType );

        /// <summary>
        /// Gets all <see cref="MemberInfo"/> that this <see cref="ICustomAttributeMultiProvider"/> handles.
        /// </summary>
        /// <returns>Enumeration of members.</returns>
        IEnumerable<MemberInfo> GetMembers();

    }
}
