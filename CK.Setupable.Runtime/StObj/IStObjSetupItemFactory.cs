#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Runtime\StObj\IStObjSetupItemFactory.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace CK.Setup
{
    /// <summary>
    /// Factory for items associated to <see cref="IStObjResult"/>.
    /// </summary>
    public interface IStObjSetupItemFactory
    {
        /// <summary>
        /// Creates an <see cref="IStObjSetupItem"/> from a <see cref="IStObjSetupData"/>.
        /// Returning null here triggers an attempt to instantiate an object of the type <see cref="IStObjSetupData.ItemType"/>
        /// with the same parameters as this method (the monitor and the data). If no <see cref="IStObjSetupData.ItemType"/> is set,
        /// a <see cref="StObjDynamicPackageItem"/> is instanciated.
        /// </summary>
        /// <param name="monitor">Monitor to use.</param>
        /// <param name="data">Descriptive data (initialized by attributes and other direct code-first approaches and configured by any <see cref="IStObjSetupConfigurator"/>).</param>
        /// <returns>A <see cref="IStObjSetupItem"/> implementation that must be correctly initialized by the given data, or null to use the default <see cref="StObjDynamicPackageItem"/>.</returns>
        IStObjSetupItem CreateSetupItem( IActivityMonitor monitor, IStObjSetupData data );
    }

}
