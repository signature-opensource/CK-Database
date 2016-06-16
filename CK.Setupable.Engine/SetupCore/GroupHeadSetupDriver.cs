#region Proprietary License
/*----------------------------------------------------------------------------
* This file (CK.Setupable.Engine\SetupCore\GroupHeadSetupDriver.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    sealed class GroupHeadSetupDriver : DriverBase
    {
        SetupItemDriver _group;

        internal GroupHeadSetupDriver( ISetupEngine center, ISortedItem<ISetupItem> sortedItem, VersionedName externalVersion )
            : base( center, sortedItem, externalVersion )
        {
        }

        internal override bool IsGroupHead
        {
            get { return true; }
        }

        public SetupItemDriver Group
        {
            get { return _group; }
            internal set { _group = value; }
        }

        internal override sealed bool ExecuteInit()
        {
            return Group.ExecuteHeadInit();
        }

        internal override sealed bool ExecuteInstall()
        {
            return Group.ExecuteHeadInstall();
        }

        internal override sealed bool ExecuteSettle()
        {
            return Group.ExecuteHeadSettle();
        }

    }
}
