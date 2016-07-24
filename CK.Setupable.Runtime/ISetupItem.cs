#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\ISetupItem.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// A setup item is an <see cref="IDependentItem"/> and a <see cref="IContextLocNaming"/>: its FullName 
    /// is structured with the Context-Location-Name triplet.
    /// It is most obten bound to an actual model object (<see cref="ActualObject"/>).
    /// </summary>
    public interface ISetupItem : IDependentItem, IContextLocNaming
    {
        /// <summary>
        /// This property is defined here to resolve ambiguity between <see cref="IDependentItem.FullName"/> 
        /// and <see cref="IContextLocNaming.FullName"/>: they are actually the same.
        /// </summary>
        new string FullName { get; }

    }
}
