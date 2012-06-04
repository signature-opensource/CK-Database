using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Setup.Tests
{
    class Item : IDependentItem, IDependentItemDiscoverer
    {
        List<string> _requires;
        List<string> _requiredBy;
        List<IDependentItem> _relatedItems;

        public Item( string fullName, params object[] content )
        {
            _requires = new List<string>();
            _requiredBy = new List<string>();
            FullName = fullName;
            Add( content );
        }

        public virtual void Add( params object[] content )
        {
            if( content == null ) return;
            foreach( object o in content )
            {
                if( o != null && o is string )
                {
                    string dep = (string)o;
                    if( dep.StartsWith( "<=" ) )
                    {
                        _requiredBy.Add( dep.Substring( 2 ).Trim() );
                    }
                    else if( dep.StartsWith( "∈" ) )
                    {
                        Container = new DependentItemContainerRef( dep.Substring( 1 ).Trim() );
                    }
                    else
                    {
                        if( dep.StartsWith( "=>" ) ) dep = dep.Substring( 2 );
                        dep = dep.Trim();
                        _requires.Add( dep );
                    }
                }
            }
        }

        public IDependentItemContainerRef Container { get; set; }

        public string FullName { get; set; }

        public IEnumerable<string> Requires { get { return _requires; } }

        public IEnumerable<string> RequiredBy { get { return _requiredBy; } }

        public IList<IDependentItem> RelatedItems
        {
            get { return _relatedItems ?? (_relatedItems = new List<IDependentItem>()); }
        }

        IEnumerable<IDependentItem> IDependentItemDiscoverer.GetOtherItemsToRegister()
        {
            return _relatedItems;
        }

        public override string ToString()
        {
            return FullName;
        }

        bool IDependentItemRef.Optional
        {
            get { return false; }
        }

    }

}
