using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface ISetupItem : IDependentItem, IContextLocName
    {
        /// <summary>
        /// Resolves ambiguity between <see cref="IDependentItem.FullName"/> and <see cref="IContextLocName.FullName"/>:
        /// they are actually the same.
        /// </summary>
        new string FullName { get; }
    }
}
