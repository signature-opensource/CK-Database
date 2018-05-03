#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\IDriverList.cs) is part of CK-Database. 
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
    /// An ordered list of <see cref="SetupItemDriver"/> indexed by the <see cref="IDependentItem.FullName"/> or 
    /// by the <see cref="IDependentItem"/> object instance itself.
    /// </summary>
    public interface IDriverList : IReadOnlyList<SetupItemDriver>
    {
        /// <summary>
        /// Gets a <see cref="SetupItemDriver"/> by its name.
        /// </summary>
        /// <param name="fullName">The item full name. Can be null: null is returned.</param>
        /// <returns>The associated driver or null if the driver does not exist.</returns>
        SetupItemDriver this[string fullName] { get; }

        /// <summary>
        /// Gets a <see cref="SetupItemDriver"/> associated to a <see cref="IDependentItem"/>.
        /// </summary>
        /// <param name="item">The item. Can be null: null is returned.</param>
        /// <returns>The associated driver or null if the driver does not exist.</returns>
        SetupItemDriver this[IDependentItem item] { get; }

    }
}
