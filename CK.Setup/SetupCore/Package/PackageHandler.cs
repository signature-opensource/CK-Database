using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class PackageHandler : ContainerHandler
    {
        protected PackageHandler( PackageDriver d )
            : base( d )
        {
        }

        protected new PackageDriver Driver { get { return (PackageDriver)base.Driver; } }

    }
}
