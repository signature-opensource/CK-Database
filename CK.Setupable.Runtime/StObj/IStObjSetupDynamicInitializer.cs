using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Dynamic initialization is the last step: the StObj have been initialized, ordered, and their corresponding <see cref="IMutableSetupItem"/> have been 
    /// created and configured. This is where new <see cref="IDependentItem"/>s can be created and registered (typically as children of the item).
    /// This interface can be supported by attributes on the structured object, by the object itself or injected in <see cref="StObjSetupItemBuilder"/>.
    /// </summary>
    public interface IStObjSetupDynamicInitializer
    {
        /// <summary>
        /// When called, any dependent StObjs have already been initialized: initializers for a Generalization are called before the ones of its Specializations for instance.
        /// If an initializer requires its dependent object to be initialized (before some of its own initializations), it can use the <see cref="IStObjSetupDynamicInitializerState.PushAction">PushAction</see>
        /// of the <paramref name="state"/> to register actions that will be executed in revert order.
        /// </summary>
        /// <param name="state">Context for dynamic initialization.</param>
        /// <param name="item">The setup item for the object slice.</param>
        /// <param name="stObj">The StObj (the object slice).</param>
        /// <remarks>
        /// When implemented by the structured object itself, it may be called multiple times: once for each StObj slice in 
        /// its hierarchy.
        /// </remarks>
        void DynamicItemInitialize( IStObjSetupDynamicInitializerState state, IMutableSetupItem item, IStObjRuntime stObj );
    }


}
