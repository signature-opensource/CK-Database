using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Tests
{
    class SetupableItemContainer : SetupableItem, ISetupableItemContainer
    {
        List<IDependentItem> _children = new List<IDependentItem>();

        public SetupableItemContainer( string fullName, params object[] content )
            : base( fullName, content )
        {
        }

        public IEnumerable<IDependentItemRef> Children
        {
            get { return _children; }
        }

        public override void Add( params object[] content )
        {
            base.Add( content );
            foreach( object o in content )
            {
                Item i = o as Item;
                if( i != null )
                {
                    _children.Add( i );
                    i.Container = this;
                }
            }
        }
    }
}
