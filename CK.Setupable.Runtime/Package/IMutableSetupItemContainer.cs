#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IMutableSetupItemContainer.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// A mutable version of an <see cref="ISetupItem"/> that is a <see cref="IDependentItemContainerTyped"/>.
/// The <see cref="IDependentItem.FullName"/> (that identifies the item) and the <see cref="IDependentItemContainerTyped.ItemKind">ItemKind</see> can not be changed through this interface.
/// </summary>
public interface IMutableSetupItemContainer : IMutableSetupItemGroup, IDependentItemContainerTyped
{
}
