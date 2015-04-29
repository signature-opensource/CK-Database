using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Setup
{
    /// <summary>
    /// A setup item that is bound to an actual object that may be a StObj (in this case <see cref="StObj"/> is not null) 
    /// or an independent object instance (typically an object subordinated to a StObj).
    /// </summary>
    public interface IStObjSetupItem : IMutableSetupItem
    {
        /// <summary>
        /// Gets the StObj. Null if this item is directly bound to an object.
        /// </summary>
        IStObjResult StObj { get; }

        /// <summary>
        /// Gets the associated object instance (the final, most specialized, structured object) when this is bound to a StObj (<see cref="StObj"/> is not null). 
        /// Otherwise gets the object associated explicitely when this setup item has been created.
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The function that is injected during the graph creation (at the StObj level) simply returns the <see cref="IStObjResult.StObj"/> instance that is NOT always a "real",
        /// fully operational, object since its auto implemented methods (or other aspects) have not been generated yet.
        /// </para>
        /// <para>
        /// Once the final assembly has been generated, this function is updated with <see cref="IContextualStObjMap.Obtain"/>: during the setup phasis, the actual 
        /// objects that are associated to items are "real" objects produced/managed by the final <see cref="StObjContextRoot"/>.
        /// </para>
        /// <para>
        /// In order to honor potential transient lifetime (one day), these object should not be aggressively cached, this is why this is a <see cref="GetObject()"/> function 
        /// and not a simple 'Object' or 'FinalObject' property. 
        /// </para>
        /// </remarks>
        object GetObject();
    }
}
