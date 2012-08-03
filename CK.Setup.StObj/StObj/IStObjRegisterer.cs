using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    public interface IStObjRegisterer
    {
        /// <summary>
        /// Gets the <see cref="Type"/> that identifies this context. Null for default context.
        /// </summary>
        Type Context { get; }

        /// <summary>
        /// Gets the logger to use.
        /// </summary>
        IActivityLogger Logger { get; }

        /// <summary>
        /// Registers a <see cref="IStObjDependentItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="IStObjDependentItem"/> to register.</param>
        void Register( IStObjDependentItem item );
    }
}
