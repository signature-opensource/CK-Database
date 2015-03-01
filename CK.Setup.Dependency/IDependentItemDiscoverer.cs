#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setup.Dependency\IDependentItemDiscoverer.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace CK.Setup
{
    /// <summary>
    /// A discoverer is able to provide a set of <see cref="IDependentItem"/>. See remarks.
    /// </summary>
    /// <remarks>
    /// This is an optional interface for <see cref="IDependentItem"/> objects: by implementing this
    /// an item can automatically register a set of items when it is itself registered
    /// by <see cref="G:DependencySorter.OrderItems"/>.
    /// </remarks>
    public interface IDependentItemDiscoverer
    {
        /// <summary>
        /// Gets a list of <see cref="IDependentItem"/> that must participate to the 
        /// setup. Can be null if no such item exists.
        /// </summary>
        IEnumerable<IDependentItem> GetOtherItemsToRegister();
    }
}

