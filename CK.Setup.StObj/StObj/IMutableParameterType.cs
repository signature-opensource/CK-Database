using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Describes a parameter of a Construct method.
    /// </summary>
    public interface IMutableParameterType : IMutableReferenceType
    {
        /// <summary>
        /// Gets the parameter position in the list.
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
        /// Gets or sets whether this parameter can be considered as optional.
        /// Defaults to <see cref="IsRealParameterOptional"/>.
        /// When changed by <see cref="IStObjExternalConfigurator"/> from false to true (the formal parameter is NOT optional), null becomes
        /// an acceptable value for the parameter, otherwise null is not accepted.
        /// </summary>
        bool IsOptional { get; set; }

    }
}
