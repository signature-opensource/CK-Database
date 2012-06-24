using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup
{
    public class VersionedItemDriver : ItemDriver
    {
        public VersionedItemDriver( BuildInfo info )
            : base( info )
        {
            if( !(info.SortedItem.Item is IVersionedItem) ) throw new InvalidOperationException( "Attempt to build a VersionedItemDriver for an item that is not a IVersionedItem." );
        }

        /// <summary>
        /// Gets the <see cref="IVersionedItem"/> to setup.
        /// </summary>
        public new IVersionedItem Item
        {
            get { return (IVersionedItem)base.Item; }
        }

    }
}
