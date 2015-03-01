#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Version\VersionedNameExtension.cs) is part of CK-Database. 
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
    /// Offers extension for <see cref="VersionedName"/> related objects.
    /// </summary>
    public static class VersionedNameExtension
    {
        /// <summary>
        /// Returns a collection of <see cref="VersionedName"/> with potentially different <see cref="VersionedName.FullName"/>.
        /// Can safely be called on null source reference.
        /// </summary>
        /// <param name="source">This enumerable.</param>
        /// <param name="fullNameProvider">Name provider. By returning null, the VersionedName does not appear in the resulting enumerable.</param>
        /// <returns>A collection of <see cref="VersionedName"/> whose FullName may have changed.</returns>
        public static IEnumerable<VersionedName> SetRefFullName( this IEnumerable<VersionedName> source, Func<VersionedName, string> fullNameProvider )
        {
            if( source != null )
            {
                foreach( var v in source )
                {
                    string newName = fullNameProvider( v );
                    if( newName != null )
                    {
                        if( newName == v.FullName ) yield return v;
                        else yield return new VersionedName( newName, v.Version );
                    }
                }
            }
        }
    }
}
