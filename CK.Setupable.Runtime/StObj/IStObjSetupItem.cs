using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// A setup item that is bound to a StObj.
    /// </summary>
    public interface IStObjSetupItem : IMutableSetupItem, ISetupObjectItem
    {
        /// <summary>
        /// Gets the StObj. Null if this item is directly bound to an object.
        /// </summary>
        IStObjResult StObj { get; }

    }
}
