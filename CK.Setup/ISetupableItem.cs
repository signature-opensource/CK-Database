using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public interface ISetupableItem : IDependentItem, IVersionedItem
    {
        /// <summary>
        /// Gets a name that uniquely identifies the item. It must be not null.
        /// This is a redifinition to remove the ambiguity between <see cref="IDependentItem.FullName"/>
        /// and <see cref="IVersionedItem.FullName"/> that are actually the same.
        /// </summary>
        new string FullName { get; }

        /// <summary>
        /// Gets the type name of the driver in charge of the setup.
        /// The driver is loosely coupled to its item.
        /// </summary>
        string SetupDriverTypeName { get; }
    }
}
