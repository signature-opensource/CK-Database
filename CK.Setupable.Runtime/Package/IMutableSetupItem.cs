#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\Package\IMutableSetupItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion


namespace CK.Setup;

/// <summary>
/// A mutable version of an <see cref="ISetupItem"/> thats supports <see cref="Generalization"/>
/// and different <see cref="ItemKind"/>.
/// Note that the specialized <see cref="IMutableSetupItemGroup"/> offers children collection.
/// Its <see cref="IContextLocNaming.Context">Context</see>, <see cref="IContextLocNaming.Location">Location</see>, <see cref="IContextLocNaming.Name">Name</see> 
/// and <see cref="ISetupItem.FullName">FullName</see> (that identify the item) and <see cref="ItemKind"/> can not be changed through this interface.
/// </summary>
public interface IMutableSetupItem : IMutableSetupBaseItem
{
    /// <summary>
    /// Gets whether this object must be considered as a <see cref="IDependentItem"/>, a <see cref="IDependentItemGroup"/> or a <see cref="IDependentItemContainer"/>
    /// whatever its actual type is.
    /// </summary>
    DependentItemKind ItemKind { get; }

    /// <summary>
    /// Gets or sets the generalization of this item.
    /// </summary>
    new IDependentItemRef Generalization { get; set; }

}
