using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    /// <summary>
    /// Driver for <see cref="IPackage"/>.
    /// </summary>
    public class PackageDriver : ContainerDriver
    {
        public PackageDriver( BuildInfo info )
            : base( info )
        {
            if( !(info.SortedItem.Item is IPackage) ) throw new InvalidOperationException( "Attempt to build a PackageDriver for an item that is not a IPackage." );
        }

        /// <summary>
        /// Gets the package to setup.
        /// </summary>
        public new IPackage Item
        {
            get { return (IPackage)base.Item; }
        }


    }
}
