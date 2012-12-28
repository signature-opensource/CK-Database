using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup item is an <see cref="IDependentItem"/> and a <see cref="IContextLocNaming"/>: its FullName is structured with the Context-Location-Name triplet.
    /// </summary>
    public interface ISetupItem : IDependentItem, IContextLocNaming
    {
        /// <summary>
        /// Resolves ambiguity between <see cref="IDependentItem.FullName"/> and <see cref="IContextLocNaming.FullName"/>:
        /// they are actually the same.
        /// </summary>
        new string FullName { get; }
    }
}
