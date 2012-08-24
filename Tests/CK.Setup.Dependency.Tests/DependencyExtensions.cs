using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CK.Setup.Tests.Dependencies
{
    static class DependencyExtensions
    {
        public static IEnumerable<string> OrderedFullNames( this DependencySorterResult @this )
        {
            return @this.SortedItems.Select( o => o.FullName );
        }

        public static bool IsOrdered( this DependencySorterResult @this, params string[] fullNames )
        {
            return OrderedFullNames( @this ).SequenceEqual( fullNames );
        }
        
        public static void AssertOrdered( this DependencySorterResult @this, params string[] fullNames )
        {
            if( !OrderedFullNames( @this ).SequenceEqual( fullNames ) )
            {
                Assert.Fail( "Expecting '{0}' but was '{1}'.", String.Join( ", ", fullNames ), String.Join( ", ", OrderedFullNames( @this ) ) );
            }
        }
    }
}
