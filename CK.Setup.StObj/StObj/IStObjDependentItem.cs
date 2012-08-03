using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Defines an <see cref="IDependentItem"/> bound to a Type that is an <see cref="IAmbiantContract"/>.
    /// </summary>
    public interface IStObjDependentItem : IDependentItem
    {
        /// <summary>
        /// Gets the associated type.
        /// </summary>
        Type ItemType { get; }

        /// <summary>
        /// Must initialize this item: this is called once a <see cref="StObjCollector"/> has finished its job.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="mapper"></param>
        void InitDependentItem( IActivityLogger logger, IStObjMapper mapper );
    }
}
