using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public sealed class GroupHeadSetupDriver : DriverBase
    {
        SetupDriver _group;

        internal GroupHeadSetupDriver( SetupEngine center, ISortedItem sortedItem, VersionedName externalVersion )
            : base( center, sortedItem, externalVersion, null )
        {
        }

        internal override bool IsGroupHead
        {
            get { return true; }
        }

        public SetupDriver Group
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
