using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Captures mapping in a <see cref="IStObjMap"/>.
    /// </summary>
    public struct StObjMapMapping
    {
        /// <summary>
        /// The type mapped.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// The context name.
        /// </summary>
        public readonly string Context;

        /// <summary>
        /// The final implementation instance.
        /// </summary>
        public readonly object Implementation;

        internal StObjMapMapping( Type t, string c, object i )
        {
            Type = t;
            Context = c;
            Implementation = i;
        }
    }

}
