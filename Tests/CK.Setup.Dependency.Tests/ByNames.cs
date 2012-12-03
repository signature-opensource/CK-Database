using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class ByNames
    {
        [Test]
        public void NamesAreCaseSensitive()
        {
            var cA = new TestableContainer( "A", "⊐ b" );
            var cB = new TestableContainer( "B" );
            {
                // Starting by CA.
                var r = DependencySorter.OrderItems( cA, cB );
                Assert.That( !r.IsComplete );
                Assert.That( r.HasStructureError && r.StructureErrorCount == 1 );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void HomonymsByRequires()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var item1 = new TestableItem( "A" );
                var item2 = new TestableItem( "A" );
                item2.Requires.Add( item1 );
                {
                    var r = DependencySorter.OrderItems( item2, item1 );
                    Assert.That( !r.IsComplete );
                    Assert.That( r.HasStructureError );
                    ResultChecker.SimpleCheck( r );
                }
                {
                    var r = DependencySorter.OrderItems( item1, item2 );
                    Assert.That( !r.IsComplete );
                    Assert.That( r.HasStructureError );
                    ResultChecker.SimpleCheck( r );
                }
                {
                    var r = DependencySorter.OrderItems( item1 );
                    Assert.That( r.IsComplete );
                    ResultChecker.SimpleCheck( r );
                }

            }
        }

    }
}
