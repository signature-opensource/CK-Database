using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CK.Core
{
    /// <summary>
    /// Specialized <see cref="ICustomAttributeProvider"/> bound to a <see cref="Type"/>.
    /// The behavior when a attriutes are member that does not belong to the bouded type is retrieved
    /// </summary>
    public interface ICustomAttributeTypeProvider : ICustomAttributeProvider
    {
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
