using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// marked as beeing NOT optional OR the formal parameter of the Construct method is NOT optional), the default value for the type 
        /// is automatically used (null for reference types) if resolution fails.
        /// </summary>
        bool IsOptional { get; set; }

    }
}
