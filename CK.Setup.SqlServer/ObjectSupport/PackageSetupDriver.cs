using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using CK.Core;

namespace CK.Setup.SqlServer
{
    public class PackageSetupDriver : ContainerDriver
    {
        public PackageSetupDriver( BuildInfo info )
            : base( info )
        {
        }

        public new Package Item
        {
            get { return (Package)base.Item; }
        }


    }
}

