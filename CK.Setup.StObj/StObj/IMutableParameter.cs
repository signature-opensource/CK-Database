using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    public interface IMutableParameter : IMutableResolvableReference
    {
        /// <summary>
        /// Gets the parameter position in the list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets whether the formal parameter is optional (<see cref="Type.Missing"/> can be used as the parameter value 
        /// at invocation time, see <see cref="ParameterInfo.IsOptional"/>).
        /// </summary>
        bool IsRealParameterOptional { get; }
        
    }
}
