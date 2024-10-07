#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IPackageItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// A package is a <see cref="ISetupItem"/>, a <see cref="IDependentItemContainer"/> (it can contain
/// children) and a <see cref="IVersionedItem"/> (it is version-ed).
/// </summary>
public interface IPackageItem : ISetupItem, IDependentItemContainer, IVersionedItem
{
}
