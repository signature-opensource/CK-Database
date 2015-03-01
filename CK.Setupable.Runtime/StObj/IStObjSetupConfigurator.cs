#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\StObj\IStObjSetupConfigurator.cs) is part of CK-Database. 
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
    /// Enables configuration of <see cref="IMutableStObjSetupData"/> before setup process.
    /// </summary>
    /// <remarks>
    /// This interface can be implemented by different kind of objects:
    /// <list type="number">
    /// <item>
    ///     <term>By an Attribute that is applied to Structured Object classes.</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called right after the object instanciation (for the corresponding "slice").</description>
    /// </item>
    /// <item>
    ///     <term>By the Structured Object class itself.</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called after the ones of the attributes (for each "slice" of the object, from top most base class to the most specialized one).</description>
    /// </item>
    /// <item>
    ///     <term>As a parameter to the <see cref="StObjSetupItemBuilder"/>.</term>
    ///     <description>Its <see cref="ConfigureDependentItem"/> will be called last for all StObj beeing setup (for each "slice" of each object).</description>
    /// </item>
    /// </list>
    /// </remarks>
    public interface IStObjSetupConfigurator
    {
        /// <summary>
        /// Configures the given <see cref="IMutableStObjSetupData"/> before it participates in setup.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Mutable data (typically initialized by attributes and other direct code-first approaches).</param>
        void ConfigureDependentItem( IActivityMonitor monitor, IMutableStObjSetupData data );
    }

}
