using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public sealed class ContainerHeadDriver : DriverBase
    {
        ContainerDriver _container;

        internal ContainerHeadDriver( SetupEngine center, ISortedItem sortedItem, VersionedName externalVersion )
            : base( center, sortedItem, externalVersion, null )
        {
        }

        public override bool IsContainerHead
        {
            get { return true; }
        }

        public ContainerDriver Container
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
