using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests
{
    class TestableItem : IDependentItem, IDependentItemDiscoverer, IDependentItemRef
    {
        string _fullName;
        List<IDependentItemRef> _requires;
        List<IDependentItemRef> _requiredBy;
        List<IDependentItem> _relatedItems;
        int _startDependencySortCount;

        static int _ignoreCheckedCount = 0;
        public static IDisposable IgnoreCheckCount()
        {
            ++_ignoreCheckedCount;
            return Util.CreateDisposableAction( () => --_ignoreCheckedCount );
        }

        public TestableItem( string fullName, params object[] content )
        {
            _requires = new List<IDependentItemRef>();
            _requiredBy = new List<IDependentItemRef>();
            FullName = fullName;
            Add( content );
            if( _ignoreCheckedCount > 0 ) _startDependencySortCount = -1;
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
                        _requiredBy.Add( new NamedDependentItemRef( dep.Substring( 2 ).Trim() ) );
                    }
                    else if( dep.StartsWith( "∈" ) )
                    {
                        Container = new NamedDependentItemContainerRef( dep.Substring( 1 ).Trim() );
                    }
                    else
                    {
                        if( dep.StartsWith( "=>" ) ) dep = dep.Substring( 2 );
                        _requires.Add( new NamedDependentItemRef( dep.Trim() ) );
                    }
                }
            }
        }

        public IDependentItemContainerRef Container { get; set; }

        public void CheckStartDependencySortCountAndReset()
        {
            if( _startDependencySortCount != -1 )
            {
                Assert.That( _startDependencySortCount, Is.EqualTo( 1 ), "StartDependencySort must have been called once and only once." );
            }
            _startDependencySortCount = 0;
        }

        public string FullName 
        {
            get 
            {
                if( _startDependencySortCount != -1 )
                {
                    Assert.That( _startDependencySortCount, Is.EqualTo( 1 ), "StartDependencySort must have been called once and only once." );
                }
                return _fullName; 
            }
            set { _fullName = value; } 
        }

        public IList<IDependentItemRef> Requires { get { return _requires; } }

        public IList<IDependentItemRef> RequiredBy { get { return _requiredBy; } }

        IEnumerable<IDependentItemRef> IDependentItem.Requires { get { return _requires; } }

        IEnumerable<IDependentItemRef> IDependentItem.RequiredBy { get { return _requiredBy; } }

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

        object IDependentItem.StartDependencySort()
        {
            if( _startDependencySortCount != -1 )
            {
                Assert.That( _startDependencySortCount, Is.EqualTo( 0 ), "StartDependencySort must be called once and only once." );
                ++_startDependencySortCount;
            }
            return _startDependencySortCount;
        }
    }

}
