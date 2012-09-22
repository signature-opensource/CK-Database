using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Basic support for declarative dependency structure between types.
    /// </summary>
    public interface IStObjAttribute
    {
        /// <summary>
        /// Gets the container of the object.
        /// </summary>
        Type Container { get; }

        /// <summary>
        /// Gets an array of direct dependencies.
        /// </summary>
        Type[] Requires { get; }

        /// <summary>
        /// Gets an array of types that depends on the object.
        /// </summary>
        Type[] RequiredBy { get; }
    }
}
