using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Service type descriptor exists only if the type is not excluded (excluding a
    /// service type is like removing the <see cref="IAmbientService"/> interface marker from
    /// its interfaces) and has at least one implementation that <see cref="AmbientServiceClassInfo.IsIncluded"/>.
    /// </summary>
    public class AmbientServiceInterfaceInfo
    {
        /// <summary>
        /// The interface type.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets this Service interface life time.
        /// This reflects the <see cref="IAmbientService"/> or <see cref="ISingletonAmbientService"/>
        /// vs. <see cref="IScopedAmbientService"/> interface marker.
        /// This can never be <see cref="AmbientTypeKindExtension.IsNoneOrInvalid(AmbientTypeKind)"/> since
        /// in such cases, the AmbientServiceInterfaceInfo is not instanciated.
        /// </summary>
        public AmbientTypeKind DeclaredLifetime { get; }

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
        /// Gets the final resolved class that implements this interface.
        /// This is set at the end of the process during interface resolution and
        /// is used to handle actual lifetime checks without requiring another
        /// dictionary index.
        /// </summary>
        public AmbientServiceClassInfo FinalResolved { get; internal set; }

        /// <summary>
        /// Gets the base service interfaces that are specialized by this one.
        /// Never null and often empty.
        /// </summary>
        public readonly IReadOnlyList<AmbientServiceInterfaceInfo> Interfaces;

        /// <summary>
        /// Overridden to return a readable string.
        /// </summary>
        /// <returns>Readable string.</returns>
        public override string ToString() => $"{(IsSpecialized ? "[Specialized]" : "")}{Type.Name}";


        internal AmbientServiceInterfaceInfo( Type t, AmbientTypeKind lt, IEnumerable<AmbientServiceInterfaceInfo> baseInterfaces )
        {
            Debug.Assert( lt == AmbientTypeKind.IsAmbientService || lt == AmbientTypeKind.AmbientSingleton || lt == AmbientTypeKind.AmbientScope );
            Type = t;
            DeclaredLifetime = lt;
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

    }
}
