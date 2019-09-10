using CK.Core;
using System;

namespace CK.Setup
{
    /// <summary>
    /// Defines the "services" kind and life times and invalid combination of
    /// <see cref="IAutoService"/> and <see cref="IRealObject"/>.
    /// </summary>
    [Flags]
    public enum AutoRealTypeKind
    {
        /// <summary>
        /// Not a service we handle or external service for which
        /// no lifetime is known.
        /// </summary>
        None,

        /// <summary>
        /// Auto service flag. 
        /// </summary>
        IsAutoService = 1,

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
        /// A singleton auto service: <see cref="IsAutoService"/> | <see cref="IsSingleton"/>. 
        /// </summary>
        AutoSingleton = IsAutoService | IsSingleton,

        /// <summary>
        /// A real object is a singleton. 
        /// </summary>
        RealObject = IsSingleton | 8,

        /// <summary>
        /// A scoped auto service: <see cref="IsAutoService"/> | <see cref="IsScoped"/>.
        /// </summary>
        AutoScoped = IsAutoService | IsScoped,
    }

    /// <summary>
    /// Extends <see cref="AutoRealTypeKind"/>.
    /// </summary>
    public static class AutoRealTypeKindExtension
    {
        /// <summary>
        /// Returns a string that correctly handles flags and results to <see cref="GetAmbientKindCombinationError(AutoRealTypeKind,bool)"/>
        /// if this kind is invalid.
        /// </summary>
        /// <param name="this">This ambnient type kind.</param>
        /// <param name="realObjectCanBeSingletonService">True for Class type (not for interface).</param>
        /// <returns>A readable string.</returns>
        public static string ToStringClear( this AutoRealTypeKind @this, bool realObjectCanBeSingletonService = false )
        {
            switch( @this )
            {
                case AutoRealTypeKind.None: return "None";
                case AutoRealTypeKind.RealObject: return "RealObject";
                case AutoRealTypeKind.AutoSingleton: return "SingletonAutoService";
                case AutoRealTypeKind.AutoScoped: return "ScopedAutoService";
                case AutoRealTypeKind.IsScoped: return "ScopedService";
                case AutoRealTypeKind.IsSingleton: return "SingletonService";
                case AutoRealTypeKind.IsAutoService: return "AutoService";
                default:
                    {
                        if( realObjectCanBeSingletonService && @this == (AutoRealTypeKind.RealObject|AutoRealTypeKind.AutoSingleton) )
                        {
                            return "RealObject and AutoSingleton";
                        }
                        return GetAmbientKindCombinationError( @this );
                    }
            }
        }

        /// <summary>
        /// Gets whether this <see cref="AutoRealTypeKind"/> is <see cref="AutoRealTypeKind.None"/> or
        /// is invalid (see <see cref="GetAmbientKindCombinationError(AutoRealTypeKind,bool)"/>).
        /// </summary>
        /// <param name="this">This ambient kind.</param>
        /// <returns>whether this is invalid.</returns>
        public static bool IsNoneOrInvalid( this AutoRealTypeKind @this )
        {
            return @this == AutoRealTypeKind.None || GetAmbientKindCombinationError( @this ) != null;
        }

        /// <summary>
        /// Gets the conflicting duplicate status message or null if this ambient kind is valid.
        /// </summary>
        /// <param name="this">This ambient kind.</param>
        /// <param name="ambientObjectCanBeSingletonService">True for Class type (not for interface).</param>
        /// <returns>An error message or null.</returns>
        public static string GetAmbientKindCombinationError( this AutoRealTypeKind @this, bool ambientObjectCanBeSingletonService = false )
        {
            bool isAutoScoped = (@this & AutoRealTypeKind.AutoScoped) == AutoRealTypeKind.AutoScoped;
            bool isAutoSingleton = (@this & AutoRealTypeKind.AutoSingleton) == AutoRealTypeKind.AutoSingleton;
            bool isRealObject = (@this & AutoRealTypeKind.RealObject) == AutoRealTypeKind.RealObject;
            string conflict = null;
            if( isAutoScoped && isAutoSingleton )
            {
                if( isRealObject ) conflict = "AutoScoped, AutoSingleton and RealObject";
                else conflict = "AutoScoped and AutoSingleton";
            }
            else if( isAutoScoped && isRealObject )
            {
                conflict = "AutoScoped and RealObject";
            }
            else if( isAutoSingleton && isRealObject && !ambientObjectCanBeSingletonService )
            {
                conflict = "AutoSingleton and RealObject";
            }
            return conflict == null ? null : $"Invalid Ambient type combination: {conflict} connot be defined simultaneously."; 
        }
    }
}
