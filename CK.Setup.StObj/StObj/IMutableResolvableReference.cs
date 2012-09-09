using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A resolvable reference is either a <see cref="IMutableParameter"/> or a <see cref="IMutableAmbiantProperty"/>.
    /// </summary>
    public interface IMutableResolvableReference : IMutableReference
    {
        /// <summary>
        /// Gets the name of the reference (construct parameter or ambiant property name).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets whether the resolution of this reference can be considered as optional.
        /// When changed by <see cref="IStObjStructuralConfigurator"/> from false to true (caution: this means that the ambiant property is statically 
        /// marked as beeing NOT optional OR the formal parameter of the method is NOT optional) and the resolution fails, the property will not be set and, 
        /// for parameter, the default value of the parameter or the default value for the parameter's type is automatically used (null for reference types).
        /// </summary>
        bool IsOptional { get; set; }

        /// <summary>
        /// Sets a value for this reference.
        /// </summary>
        /// <param name="logger">The logger to use to describe any error.</param>
        /// <param name="sourceName">The name of the "source" of this action.</param>
        /// <param name="value">Value to set.</param>
        /// <returns>True on success, false if any error occurs.</returns>
        bool SetStructuralValue( IActivityLogger logger, string sourceName, object value );
    }
}
