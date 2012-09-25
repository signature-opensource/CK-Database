using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// A package is defined as beeing both a <see cref="IDependentItemContainer"/> (it can contain
    /// children) and a <see cref="IVersionedItem"/> (it is versioned).
    /// </summary>
    public interface IPackageItem : IDependentItemContainer, IVersionedItem
    {
    }
}
