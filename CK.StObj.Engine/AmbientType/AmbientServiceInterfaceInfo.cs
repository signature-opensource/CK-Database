using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    public class AmbientServiceInterfaceInfo
    {
        bool _isSpecialized;

        /// <summary>
        /// The interface type.
        /// </summary>
        public readonly Type InterfaceType;

        /// <summary>
        /// The interface type.
        /// </summary>
        public readonly int SpecializationDepth;

        /// <summary>
        /// Gets whether this service interface is specialized at least by
        /// one other interface.
        /// </summary>
        public bool IsSpecialized => _isSpecialized;

        /// <summary>
        /// Gets the base service interfaces that are specialized by this one.
        /// Never null and often empty.
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
                iT._isSpecialized = true;
            }
            SpecializationDepth = depth;
            Interfaces = bases;
        }
    }
}
