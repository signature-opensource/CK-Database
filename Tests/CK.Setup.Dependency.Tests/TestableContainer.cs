using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Tests
{
    class TestableContainer : TestableItem, IDependentItemContainerAsk, IDependentItemContainerRef
    {
        List<IDependentItemRef> _children = new List<IDependentItemRef>();

        public TestableContainer( string fullName, params object[] content )
            : base( fullName, content )
        {
        }

        public bool ThisIsNotAContainer { get; set; }

        IEnumerable<IDependentItemRef> IDependentItemGroup.Children
        {
            get { return _children; }
        }

        public IList<IDependentItemRef> Children
        {
            get { return _children; }
        }

        public override void Add( params object[] content )
        {
            foreach( object o in content )
            {
                TestableItem i = o as TestableItem;
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
                        _children.Add( new NamedDependentItemRef( dep.Substring( 1 ).Trim() ) );
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
