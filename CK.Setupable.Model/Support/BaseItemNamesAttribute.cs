#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Model\RequiresAttribute.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;

namespace CK.Setup
{
    /// <summary>
    /// Base attribute that carries a comma separated list of item names.
    /// </summary>
    public abstract class BaseItemNamesAttribute : Attribute
    {
        /// <summary>
        /// Initializes the names.
        /// </summary>
        /// <param name="names">Comma separated list of item names. Must not be null.</param>
        protected BaseItemNamesAttribute( string names )
        {
            if( names == null ) throw new ArgumentNullException( nameof( names ) );
            ItemNames = names;
        }

        /// <summary>
        /// Gets a comma separated list of item names.
        /// </summary>
        public string ItemNames { get; }
    }

}
