#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Setup\IDriverList.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System.Collections.Generic;

namespace CK.Setup;

/// <summary>
/// An ordered list of <see cref="DriverBase"/> indexed by the <see cref="IDependentItem.FullName"/> or 
/// by the <see cref="IDependentItem"/> object instance itself.
/// </summary>
public interface IDriverBaseList : IReadOnlyList<DriverBase>
{
    /// <summary>
    /// Gets a <see cref="DriverBase"/> by its name.
    /// </summary>
    /// <param name="fullName">The item full name. Can be null: null is returned.</param>
    /// <returns>The associated driver or null if the driver does not exist.</returns>
    DriverBase? this[string? fullName] { get; }

    /// <summary>
    /// Gets a <see cref="DriverBase"/> associated to a <see cref="IDependentItem"/>.
    /// </summary>
    /// <param name="item">The item. Can be null: null is returned.</param>
    /// <returns>The associated driver or null if the driver does not exist.</returns>
    DriverBase? this[IDependentItem? item] { get; }

}
