using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A mutable version of an <see cref="ISetupItem"/> that is a <see cref="IDependentItemContainerTyped"/>.
    /// The <see cref="IDependentItem.FullName"/> (that identifies the item) and the <see cref="IDependentItemContainerTyped.ItemKind">ItemKind</see> can not be changed through this interface.
    /// </summary>
    public interface IMutableSetupItemContainer : IMutableSetupItemGroup, IDependentItemContainerTyped
    {
    }
}
