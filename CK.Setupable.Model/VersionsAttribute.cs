#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\VersionsAttribute.cs) is part of CK-Database. 
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
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
    public class VersionsAttribute : Attribute
    {
        readonly string _versions;

        /// <summary>
        /// Describes the list of available versions and optional associated previous full names with a string like: "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0"
        /// The last version must NOT define a previous name since the last version is the current one (an <see cref="ArgumentException"/> will be thrown).
        /// </summary>
        /// <param name="versionsAndPreviousNames">String like "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0".</param>
        public VersionsAttribute( string versionsAndPreviousNames )
        {
            _versions = versionsAndPreviousNames;
        }

        /// <summary>
        /// Gets a string like "1.2.4, Previous.Name = 1.3.1, A.New.Name=1.4.1, 1.5.0".
        /// </summary>
        public string VersionsString
        {
            get { return _versions; }
        }

    }
}
