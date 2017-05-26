using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Captures mapping in a <see cref="IStObjMap"/> and <see cref="IContextualStObjMap"/>: 
    /// associates a <see cref="IStObj"/> to its final implementation.
    /// </summary>
    public struct StObjImplementation
    {
        /// <summary>
        /// The StObj slice.
        /// </summary>
        public readonly IStObj StObj;

        /// <summary>
        /// The final implementation instance.
        /// </summary>
        public readonly object Implementation;

        public StObjImplementation( IStObj o, object i )
        {
            StObj = o;
            Implementation = i;
        }
    }

}
