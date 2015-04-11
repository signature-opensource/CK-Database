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
        DependentItemSetupDriver _group;

        internal GroupHeadSetupDriver( ISetupEngine center, ISortedItem sortedItem, VersionedName externalVersion )
            : base( center, sortedItem, externalVersion, null )
        {
        }

        internal override bool IsGroupHead
        {
            get { return true; }
        }

        public DependentItemSetupDriver Group
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
