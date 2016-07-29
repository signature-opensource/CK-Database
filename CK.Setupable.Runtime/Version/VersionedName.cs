#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\VersionedName.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Associates a name and a version for which it is valid. It is an immutable object.
    /// </summary>
    public class VersionedName
    {
        /// <summary>
        /// Initializes a new <see cref="VersionedName"/> with a <see cref="FullName"/> (must ne be null or empty) and 
        /// a non null <see cref="Version"/>.
        /// </summary>
        /// <param name="fullName">Name valid up to <see cref="Version"/>. It must be not null nor empty otherwise an exception is thrown.</param>
        /// <param name="version">Version for the name. Must not be null.</param>
        public VersionedName( string fullName, Version version )
        {
            if( version == null ) throw new ArgumentNullException( nameof( version ) );
            if( string.IsNullOrWhiteSpace( fullName ) ) throw new ArgumentNullException( nameof( fullName ) );
            FullName = fullName;
            Version = version;
        }

        /// <summary>
        /// Gets the full name of a versionned name.
        /// Never null or empty.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the version associated to the <see cref="FullName"/>. 
        /// Never null.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Overridden to return the "FullName - Version".
        /// </summary>
        /// <returns></returns>
        public override string ToString() => FullName + " - " + Version.ToString();

    }
}
