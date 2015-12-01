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
        string _name;
        Version _version;

        /// <summary>
        /// Initializes a new <see cref="VersionedName"/> with a <see cref="FullName"/> and a <see cref="Version"/>.
        /// Both of them must be valid.
        /// </summary>
        /// <param name="fullName">Name valid up to <see cref="Version"/>. It must be not null nor empty otherwise an exception is thrown.</param>
        /// <param name="v">Valid version for the name. Null is converted into <see cref="Util.EmptyVersion"/>.</param>
        public VersionedName( string fullName, Version v )
        {
            FullName = fullName;
            Version = v;
        }

        /// <summary>
        /// Gets the full name of a versionned name.
        /// </summary>
        public string FullName 
        {
            get { return _name; }
            private set
            {
                if( String.IsNullOrWhiteSpace( value ) ) throw new ArgumentException();
                _name = value;
            }
        }

        /// <summary>
        /// Gets the version associated to the <see cref="FullName"/>. 
        /// Never null (<see cref="Util.EmptyVersion"/> at least).
        /// </summary>
        public Version Version
        {
            get { return _version; }
            private set { _version = value ?? Util.EmptyVersion; }
        }

        /// <summary>
        /// Overridden to return the "FullName - Version".
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name + " - " + _version.ToString();
        }

    }
}
