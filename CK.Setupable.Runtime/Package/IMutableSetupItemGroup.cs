#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IMutableSetupItemGroup.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// A mutable version of an <see cref="ISetupItem"/> that is a <see cref="IDependentItemGroup"/>.
/// </summary>
public interface IMutableSetupItemGroup : IMutableSetupItem, IDependentItemGroup
{
    /// <summary>
    /// Gets a mutable list of items that this item requires.
    /// </summary>
    new IDependentItemList Children { get; }
}
