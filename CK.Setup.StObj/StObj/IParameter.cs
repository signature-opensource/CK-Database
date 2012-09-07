using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a read only parameter of a Construct method.
    /// </summary>
    public interface IParameter
    {
        /// <summary>
        /// Gets the item that uses this parameter in its Construct method.
        /// </summary>
        IStObj Owner { get; }
        
        /// <summary>
        /// Gets the typed context associated to the <see cref="P:Type"/> of this reference.
        /// </summary>
        Type Context { get; set; }

        /// <summary>
        /// Gets the type of the reference.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the zero based parameter position in the list.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets whether the formal parameter is optional (<see cref="Type.Missing"/> can be used as the parameter value 
        /// at invocation time, see <see cref="ParameterInfo.IsOptional"/>).
        /// </summary>
        bool IsRealParameterOptional { get; }

        /// <summary>
        /// Gets whether this parameter can be considered as optional.
        /// When true (even if <see cref="IsRealParameterOptional"/> is false) the default value for the type (null for reference types) is automatically used if 
        /// resolution fails (ie. <see cref="Type.Missing"/> is obtained).
        /// </summary>
       bool IsOptional { get; }

    }
}
