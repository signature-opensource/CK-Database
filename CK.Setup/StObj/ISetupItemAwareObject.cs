using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{

    /// <summary>
    /// Optional interface that can be implemented by objects in order to be coupled with their 
    /// associated <see cref="IDependentItem"/> during a setup phasis.
    /// </summary>
    public interface ISetupItemAwareObject
    {
        /// <summary>
        /// Gets or sets the associated <see cref="IDependentItem"/>.
        /// </summary>
        /// <remarks>
        /// Setting must actually be done by the framework.
        /// </remarks>
        IDependentItem SetupItem { get; set; }
    }
}
