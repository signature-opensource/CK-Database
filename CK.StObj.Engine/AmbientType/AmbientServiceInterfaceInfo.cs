using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// Gets whether this service interface is specialized at least by
        /// one other interface.
        /// </summary>
        public bool IsSpecialized { get; private set; }

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
                iT.IsSpecialized = true;
            }
            SpecializationDepth = depth;
            Interfaces = bases;
        }

        List<AmbientServiceInterfaceInfo> _unifiedBy;

        internal void CheckUnification( IActivityMonitor m )
        {
            Debug.Assert( !IsSpecialized );
            foreach( var iT in Interfaces ) iT.SetUnifier( m, this );
        }

        void SetUnifier( IActivityMonitor m, AmbientServiceInterfaceInfo u )
        {
            if( _unifiedBy == null ) _unifiedBy = new List<AmbientServiceInterfaceInfo>() { u };
            else _unifiedBy.Add( u );
            foreach( var iT in Interfaces ) iT.SetUnifier( m, this );
        }
    }
}
