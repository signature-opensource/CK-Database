#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\VersionedName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Associates a name, a version and an item type. It is an immutable object.
    /// </summary>
    public sealed class VersionedTypedName : VersionedName
    {
        /// <summary>
        /// Initializes a new <see cref="VersionedTypedName"/> with a <see cref="VersionedName.FullName"/> (must ne be null or empty) and 
        /// a not null <see cref="VersionedName.Version"/> and a type.
        /// </summary>
        /// <param name="fullName">Name valid up to <see cref="Version"/>. It must be not null nor empty otherwise an exception is thrown.</param>
        /// <param name="type">The item's <see cref="Type"/>. Must not be null or empty.</param>
        /// <param name="v">Version for the name. Must not be null.</param>
        public VersionedTypedName( string fullName, string type, Version v )
            : base( fullName, v )
        {
            if( string.IsNullOrEmpty( type ) ) throw new ArgumentNullException( nameof( type ) );
            Type = type;
        }

        /// <summary>
        /// Gets the item type of the versionned name.
        /// Never null or empty.
        /// </summary>
        public string Type { get; }


        /// <summary>
        /// Overridden to return the "FullName - Version - Type".
        /// </summary>
        /// <returns>The FullName - Version - Type.</returns>
        public override string ToString() => FullName + " - " + Version.ToString() + " - " + Type;

    }
}
