using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Defines the "services" kind and life times and invalid combination of
    /// <see cref="IAmbientService"/> and <see cref="IAmbientObject"/>.
    /// </summary>
    [Flags]
    public enum AmbientTypeKind
    {
        /// <summary>
        /// Not a service we handle or external service for which
        /// no lifetime is known.
        /// </summary>
        None,

        /// <summary>
        /// Ambient service flag. 
        /// </summary>
        IsAmbientService = 1,

        /// <summary>
        /// Singleton flag.
        /// External services are flagged with this only.
        /// </summary>
        IsSingleton = 2,

        /// <summary>
        /// Scoped flag.
        /// External services are flagged with this only.
        /// </summary>
        IsScoped = 4,

        /// <summary>
        /// A singleton ambient service: <see cref="IsAmbientService"/> | <see cref="IsSingleton"/>. 
        /// </summary>
        AmbientSingleton = IsAmbientService | IsSingleton,

        /// <summary>
        /// An ambient object is a singleton. 
        /// </summary>
        AmbientObject = IsSingleton | 8,

        /// <summary>
        /// A scoped ambient service: <see cref="IsAmbientService"/> | <see cref="IsScoped"/>.
        /// </summary>
        AmbientScope = IsAmbientService | IsScoped,
    }

    /// <summary>
    /// Extends <see cref="AmbientTypeKind"/>.
    /// </summary>
    public static class AmbientTypeKindExtension
    {
        /// <summary>
        /// Returns a string that correctly handles flags and results to <see cref="GetAmbientKindCombinationError(AmbientTypeKind)"/>
        /// if this kind is invalid.
        /// </summary>
        /// <param name="this">This ambnient type kind.</param>
        /// <returns>A readable string.</returns>
        public static string ToStringClear( this AmbientTypeKind @this )
        {
            switch( @this )
            {
                case AmbientTypeKind.None: return "None";
                case AmbientTypeKind.AmbientObject: return "AmbientObject";
                case AmbientTypeKind.AmbientSingleton: return "AmbientSingleton";
                case AmbientTypeKind.AmbientScope: return "AmbientScope";
                case AmbientTypeKind.IsScoped: return "Scoped Service";
                case AmbientTypeKind.IsSingleton: return "Singleton Service";
                default: return GetAmbientKindCombinationError( @this );
            }
        }

        /// <summary>
        /// Gets whether this <see cref="AmbientTypeKind"/> is <see cref="AmbientTypeKind.None"/> or
        /// is invalid (see <see cref="GetAmbientKindCombinationError(AmbientTypeKind)"/>).
        /// </summary>
        /// <param name="this">This ambient kind.</param>
        /// <returns>whether this is invalid.</returns>
        public static bool IsNoneOrInvalid( this AmbientTypeKind @this )
        {
            return @this == AmbientTypeKind.None || GetAmbientKindCombinationError( @this ) != null;
        }

        /// <summary>
        /// Gets the conflicting duplicate status message or null if this ambient kind is valid.
        /// </summary>
        /// <param name="this">This ambient kind.</param>
        /// <param name="ambientObjectCanBeSingletonService">True for Class type (not for interface).</param>
        /// <returns>An error message or null.</returns>
        public static string GetAmbientKindCombinationError( this AmbientTypeKind @this, bool ambientObjectCanBeSingletonService = false )
        {
            bool isAmbientScope = (@this & AmbientTypeKind.AmbientScope) == AmbientTypeKind.AmbientScope;
            bool isAmbientSingleton = (@this & AmbientTypeKind.AmbientSingleton) == AmbientTypeKind.AmbientSingleton;
            bool isAmbientObject = (@this & AmbientTypeKind.AmbientObject) == AmbientTypeKind.AmbientObject;
            string conflict = null;
            if( isAmbientScope && isAmbientSingleton )
            {
                if( isAmbientObject ) conflict = "AmbientScope, AmbientSingleton and AmbientObject";
                else conflict = "AmbientScope and AmbientSingleton";
            }
            else if( isAmbientScope && isAmbientObject )
            {
                conflict = "AmbientScope and AmbientObject";
            }
            else if( isAmbientSingleton && isAmbientObject && !ambientObjectCanBeSingletonService )
            {
                conflict = "AmbientSingleton and AmbientObject";
            }
            return conflict == null ? null : $"Invalid Ambient type combination: {conflict} connot be defined simultaneously."; 
        }
    }
}
