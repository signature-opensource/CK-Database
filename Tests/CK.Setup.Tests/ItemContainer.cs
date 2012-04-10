using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Tests
{
    class ItemContainer : Item, IDependentItemContainer
    {
        List<IDependentItemRef> _children = new List<IDependentItemRef>();

        public ItemContainer( string fullName, params object[] content )
            : base( fullName, content )
        {
        }

        public IEnumerable<IDependentItemRef> Children
        {
            get { return _children; }
        }

        public override void Add( params object[] content )
        {
            foreach( object o in content )
            {
                Item i = o as Item;
                if( i != null )
                {
                    _children.Add( i );
                    i.Container = this;
                }
                else
                {
                    string dep = (string)o;
                    if( dep.StartsWith( "∋" ) )
                    {
                        _children.Add( new DependentItemRef( dep.Substring( 1 ).Trim() ) );
                    }
                    else
                    {
                        base.Add( o );
                    }
                }
            }
        }

    }
}
