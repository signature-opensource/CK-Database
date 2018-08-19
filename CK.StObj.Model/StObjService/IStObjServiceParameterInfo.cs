using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Describes a <see cref="Value"/> that must be used for a
    /// Service class constructor parameter.
    /// </summary>
    public interface IStObjServiceParameterInfo
    {
        /// <summary>
        /// Gets the Type of this parameter.
        /// </summary>
        Type ParameterType { get; }

        /// <summary>
        /// Gets the zero-based position of the parameter in the parameter list.
        /// </summary>
        int Position { get; }

        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the value that must be built and set.
        /// Null if no actual value should be built and null must be set: the parameter
        /// allows a default value and this default value must be used (no attempt to
        /// resolve this parameter should be made).
        /// </summary>
        IStObjServiceClassFactoryInfo Value { get; }
    }

}
