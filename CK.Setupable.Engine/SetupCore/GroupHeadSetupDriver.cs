#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\GroupHeadSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using CK.Core;

namespace CK.Setup
{
    sealed class GroupHeadSetupDriver : DriverBase
    {
        internal GroupHeadSetupDriver( IDriverList drivers, IDriverBaseList allDrivers, ISortedItem<ISetupItem> sortedItem, VersionedName externalVersion )
            : base( drivers, allDrivers, sortedItem, externalVersion )
        {
        }

        internal override bool IsGroupHead => true;

        internal SetupItemDriver GroupOrContainer;

        internal override sealed bool ExecuteInit( IActivityMonitor m ) => GroupOrContainer.ExecuteHeadInit( m );

        internal override sealed bool ExecuteInstall( IActivityMonitor m ) => GroupOrContainer.ExecuteHeadInstall( m );

        internal override sealed bool ExecuteSettle( IActivityMonitor m ) => GroupOrContainer.ExecuteHeadSettle( m );

    }
}
