using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="ICustomAttributeProvider"/> bound to a <see cref="Type"/>.
    /// When requested member in calls to base <see cref="ICustomAttributeProvider"/> methods do not belong to the bounded type an exception is thrown.
    /// </summary>
    public interface ICustomAttributeTypeProvider : ICustomAttributeProvider
    {
        /// <summary>
        /// Gets whether an attribute that is assignable to the given <paramref name="attributeType"/> 
        /// exists on the bounded type.
        /// </summary>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <returns>True if at least one attribute exists.</returns>
        bool IsDefined( Type attributeType );

        /// <summary>
        /// Gets a set of attributes that are assignable to the given <see cref="attributeType"/>.
        /// </summary>
        /// <param name="attributeType">Type of requested attributes.</param>
        /// <returns>Enumeration possibly empty.</returns>
        IEnumerable<object> GetCustomAttributes( Type attributeType );

        /// <summary>
        /// Strongly typed version.
        /// </summary>
        /// <typeparam name="T">Type of the attributes.</typeparam>
        /// <returns>Enumeration possibly empty.</returns>
        IEnumerable<T> GetCustomAttributes<T>();
    }
}
