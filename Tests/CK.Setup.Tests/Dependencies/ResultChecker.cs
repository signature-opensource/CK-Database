using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CK.Core;
using NUnit.Framework;

namespace CK.Setup.Tests.Dependencies
{
    class ResultChecker
    {
        Dictionary<string, ISortedItem> _byName;

        public ResultChecker( DependencySorterResult r )
        {
            Result = r;
            _byName = r.SortedItems.ToDictionary( o => o.FullName );
        }
        
        public readonly DependencySorterResult Result;

        public void CheckRecurse( params string[] fullNames )
        {
            foreach( string s in fullNames ) Check( s );
        }

        public static void CheckMissingInvariants( DependencySorterResult r )
        {
            // Naive implementation. 
            if( r.ItemIssues.Count > 0 )
            {
                Assert.That( r.HasRequiredMissing == r.ItemIssues.Any( m => m.RequiredMissingCount > 0 ) );
                foreach( var m in r.ItemIssues )
                {
                    int optCount = 0;
                    int reqCount = 0;
                    foreach( var dep in m.MissingDependencies )
                    {
                        if( dep[0] == '?' )
                        {
                            string strong = dep.Substring( 1 );
                            Assert.That( m.MissingDependencies.Contains( strong ), Is.False ); 
                            ++optCount;
                        }
                        else
                        {
                            string weak = '?' + dep;
                            Assert.That( m.MissingDependencies.Contains( weak ), Is.False );
                            ++reqCount;
                        }
                    }
                    Assert.That( m.RequiredMissingCount == reqCount );
                    Assert.That( m.MissingDependencies.Count() == reqCount + optCount );
                }
            }
        }

        void Check( object sortedItemOrFullName )       
        {
            ISortedItem o = sortedItemOrFullName is ISortedItem ? (ISortedItem)sortedItemOrFullName : Find( (string)sortedItemOrFullName );
            if( o == null ) return;

            // If Head, then we check the head/container order and Requires and then we stop.
            if( o.IsContainerHead )
            {
                Assert.That( o.Container == o.ContainerForHead.Container, "The container is the same for a head and its associated container." );
                Assert.That( o.Index < o.ContainerForHead.Index, "{0} is before {1} (since {0} is the head of {1}).", o.FullName, o.ContainerForHead.FullName );
                // Consider the head as its container: the head must be contained in the container of our container if it exists.               
                if( o.Item.Container != null )
                {
                    ISortedItem container = Find( o.Item.Container.FullName );
                    Assert.That( container != null && container.IsContainer );
                    CheckItemInContainer( o, container );
                }
                // Requirements of a package is carried by its head.
                CheckRequires( o, o.ContainerForHead.Item.Requires );
                return;
            }
            // Checking Container.
            if( o.Item.Container != null )
            {
                ISortedItem container = Find( o.Item.Container.FullName );
                Assert.That( container != null && container.IsContainer );
                CheckItemInContainer( o, container );
            }
            
            if( o.IsContainer )
            {
                Check( o.HeadForContainer );
                foreach( IDependentItem item in ((IDependentItemContainer)o.Item).Children ) CheckRecurse( item.FullName );
                // Requirements of a package is carried by its head: we don't check Requires here.
            }
            else CheckRequires( o, o.Item.Requires );
            
            // RequiredBy applies to normal items and to container (the container itself, not its head).
            foreach( string invertReq in o.Item.RequiredBy )
            {
                var after = _byName.GetValueWithDefault( invertReq, null );
                if( after != null ) Assert.That( o.Index < after.Index, "{0} is before {1} (since {1} is required by {0}).", o.FullName, after.FullName );
            }
        }

        private void CheckRequires( ISortedItem o, IEnumerable<string> requirements )
        {
            if( requirements != null )
            {
                foreach( string dep in requirements )
                {
                    var before = Find( dep );
                    if( before != null ) Assert.That( before.Index < o.Index, "{0} is before {1} (since {1} requires {0}).", before.FullName, o.FullName );
                }
            }
        }

        private static void CheckItemInContainer( ISortedItem o, ISortedItem container )
        {
            Assert.That( container != null, "Container necessarily exists." );
            Assert.That( container.HeadForContainer.Index < o.Index, "{0} is before {1} (since {0} contains {1}).", container.HeadForContainer.FullName, o.FullName );
            Assert.That( o.Index < container.Index, "{0} is before {1} (since {1} contains {0}).", o.FullName, container.FullName );
        }

        ISortedItem Find( string fullName )
        {
            ISortedItem o;
            bool found = _byName.TryGetValue( fullName, out o );
            Assert.That( found || IsDetectedMissingDependency( fullName ), "{0} not found.", fullName );
            return o;
        }

        private bool IsDetectedMissingDependency( string fullName )
        {
            return Result.ItemIssues.Any( m => m.MissingDependencies.Contains( fullName ) );
        }

    }
}
