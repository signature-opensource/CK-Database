using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Service type descriptor exists only if the type is not excluded: excluding a
    /// service type is like removing the <see cref="IAmbientService"/> interface marker from
    /// its interfaces.
    /// </summary>
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
        /// Gets whether this service interface is specialized at least by
        /// one other interface.
        /// </summary>
        public bool IsSpecialized { get; private set; }

        /// <summary>
        /// Gets the most specialized interface that must be unique.
        /// </summary>
        public AmbientServiceInterfaceInfo MostSpecialized { get; private set; }

        /// <summary>
        /// Gets the base service interfaces that are specialized by this one.
        /// Never null and often empty.
        /// </summary>
        public readonly IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces;

        /// <summary>
        /// Overridden to return a readable string.
        /// </summary>
        /// <returns>Readable string.</returns>
        public override string ToString() => $"{(IsSpecialized ? "[Specialized]" : "")}{InterfaceType.Name}";


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
                iT.IsSpecialized = true;
            }
            SpecializationDepth = depth;
            Interfaces = bases;
        }

        internal bool CheckUnification( IActivityMonitor m )
        {
            Debug.Assert( !IsSpecialized );
            bool result = true;
            foreach( var iT in Interfaces ) result &= iT.SetMostSpecialized( m, this );
            return result;
        }

        bool SetMostSpecialized( IActivityMonitor m, AmbientServiceInterfaceInfo u )
        {
            bool result = true;
            if( MostSpecialized == null )
            {
                MostSpecialized = u;
                foreach( var iT in Interfaces ) result &= iT.SetMostSpecialized( m, u );
            }
            else if( MostSpecialized != u )
            {
                m.Error( $"Service interface '{InterfaceType.FullName}' is extended by both '{MostSpecialized.InterfaceType.FullName}' and '{u.InterfaceType.FullName}' and no unification. One of them must be excluded." );
                result = false;
            }
            return result;
        }
    }
}
