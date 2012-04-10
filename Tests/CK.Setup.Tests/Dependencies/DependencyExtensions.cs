using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
