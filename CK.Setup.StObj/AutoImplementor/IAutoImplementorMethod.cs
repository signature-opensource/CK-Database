using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CK.Core
{
    /// <summary>
    /// Classes that implement this interface are able to implement a method.
    /// </summary>
    public interface IAutoImplementorMethod
    {
        /// <summary>
        /// Implements the given method on the given <see cref="TypeBuilder"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="m"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        bool Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b );
    }

}
