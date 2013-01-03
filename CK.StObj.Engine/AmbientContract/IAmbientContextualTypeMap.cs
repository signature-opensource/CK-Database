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
    public interface IAmbientContextualTypeMap : IContextualTypeMap
    {
        /// <summary>
        /// Offers access to the <see cref="IAmbientTypeMap"/> to which this contextual mapper belongs.
        /// </summary>
        IAmbientTypeMap Owner { get; }
    }
}
