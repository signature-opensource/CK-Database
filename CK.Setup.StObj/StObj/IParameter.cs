using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Exposes a resolvable parameter.
    /// </summary>
    public interface IParameter : IResolvableReference
    {
        /// <summary>
        /// Gets the zero based parameter position in the list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets whether the formal parameter is actually optional. 
        /// When both this and <see cref="IResolvableReference.IsOptional"/> are true and <see cref="Value"/> has not been resolved, <see cref="Type.Missing"/> will be 
        /// used as the parameter value at invocation time. When this is false, the default value for the expected type is used.
        /// </summary>
        bool IsRealParameterOptional { get; }

    }
}
