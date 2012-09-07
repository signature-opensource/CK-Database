using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public sealed class GroupHeadSetupDriver : DriverBase
    {
        SetupDriver _container;

        internal GroupHeadSetupDriver( SetupEngine center, ISortedItem sortedItem, VersionedName externalVersion )
            : base( center, sortedItem, externalVersion, null )
        {
        }

        internal override bool IsGroupHead
        {
            get { return true; }
        }

        public SetupDriver Container
        {
            get { return _container; }
            internal set { _container = value; }
        }

        internal override sealed bool ExecuteInit()
        {
            return Container.ExecuteHeadInit();
        }

        internal override sealed bool ExecuteInstall()
        {
            return Container.ExecuteHeadInstall();
        }

        internal override sealed bool ExecuteSettle()
        {
            return Container.ExecuteHeadSettle();
        }

    }
}
