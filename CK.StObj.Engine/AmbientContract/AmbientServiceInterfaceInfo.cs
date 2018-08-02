using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    public class AmbientServiceInterfaceInfo
    {
        /// <summary>
        /// The interface type.
        /// </summary>
        public readonly Type InterfaceType;

        /// <summary>
        /// The interface type.
        /// </summary>
        public readonly int SpecializationDepth;

        /// <summary>
        /// Gets the supported base service interfaces. Can be empty.
        /// </summary>
        public readonly IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces;

        internal AmbientServiceInterfaceInfo( Type t, IEnumerable<AmbientServiceInterfaceInfo> baseInterfaces )
        {
            InterfaceType = t;
            AmbientServiceInterfaceInfo[] bases = Array.Empty<AmbientServiceInterfaceInfo>();
            int depth = 0;
            foreach( var iT in baseInterfaces )
            {
                depth = Math.Max( depth, iT.SpecializationDepth + 1 );
                Array.Resize( ref bases, bases.Length + 1 );
                bases[bases.Length - 1] = iT;
            }
            SpecializationDepth = depth;
            Interfaces = bases;
        }
    }
}
