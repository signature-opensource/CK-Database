using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// This interface enables an object that is a <see cref="IDependentItemContainer"/> to 
    /// dynamically choose to be a simple <see cref="IDependentItem"/>. This is for advanced use, see remarks.
    /// </summary>
    /// <remarks>
    /// This is mainly for optimization: a simple item does not generate a head. If the items to be sorted
    /// are known and no named references are used, this enables a potential container to be considered as 
    /// a dependent item if it assumes that it has no content.
    /// </remarks>
    public interface IDependentItemContainerAsk : IDependentItemContainer
    {
        bool ThisIsNotAContainer { get; }
    }
}
