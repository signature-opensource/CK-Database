using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Unifies <see cref="AmbientPropertyAttribute"/> and <see cref="AmbientContractAttribute"/>.
    /// </summary>
    public interface IAmbientPropertyOrContractAttribute
    {
        /// <summary>
        /// Gets whether resolving this property is required or not.
        /// </summary>
        bool IsOptional { get; }

        /// <summary>
        /// Gets whether tha attribute defines the <see cref="IsOptional"/> value or if it must be inherited.
        /// </summary>
        bool IsOptionalDefined { get; }

        /// <summary>
        /// Gets whether the property is an ambient property. Otherwise it is an ambient contract.
        /// </summary>
        bool IsAmbientProperty { get; }
    }
}
