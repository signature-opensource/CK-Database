using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CK.Core
{

    /// <summary>
    /// Exposes type mapping for a <see cref="Context"/>.
    /// Extends <see cref="IContextualTypeMap"/> (from CK.StObj.Model).
    /// </summary>
    public interface IAmbientTypeContextualMapper : IContextualTypeMap
    {
        /// <summary>
        /// Offers access to the <see cref="IAmbientTypeMapper"/> to which this contextual mapper belongs.
        /// </summary>
        IAmbientTypeMapper Owner { get; }

        /// <summary>
        /// Exposes internal mapping configuration.
        /// </summary>
        /// <returns>Internal mappings.</returns>
        IDictionary GetRawMappings();
    }
}
