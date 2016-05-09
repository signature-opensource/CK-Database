using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// Defines scope for <see cref="SetupItemSelectorBaseAttribute"/>.
    /// </summary>
    public enum SetupItemSelectorScope
    {
        /// <summary>
        /// No scope defined.
        /// </summary>
        None,
        /// <summary>
        /// All setup items.
        /// </summary>
        All,
        /// <summary>
        /// Only direct chilren.
        /// </summary>
        DirectChildren,
        /// <summary>
        /// All children.
        /// </summary>
        Children
    }
}
