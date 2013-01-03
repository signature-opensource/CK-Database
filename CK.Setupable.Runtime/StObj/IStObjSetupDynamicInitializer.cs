using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Dynamic initialization is the last step: the StObj have been initialized, ordered, and their corresponding <see cref="IMutableSetupItem"/> have been 
    /// created and configured. This is where new <see cref="IDependentItem"/> can be created and registered (typically as children of the <paramref name="holderSetupItem"/>).
    /// This interface can be supported by attributes on the structured object, by the object itself or injected in <see cref="StObjSetupBuilder"/>.
    /// </summary>
    public interface IStObjSetupDynamicInitializer
    {
        /// <summary>
        /// When called, any dependent StObjs have already been initialized.
        /// </summary>
        /// <param name="logger">Logger to use.</param>
        /// <param name="item">The setup item for the object slice.</param>
        /// <param name="stObj">The StObj gives detailed information on the object slice.</param>
        /// <remarks>
        /// When implemented by the structured object itself, it may be called multiple times: once for each StObj slice in 
        /// its hierarchy.
        /// </remarks>
        void DynamicItemInitialize( IActivityLogger logger, IMutableSetupItem item, IStObjRuntime stObj );
    }


}
