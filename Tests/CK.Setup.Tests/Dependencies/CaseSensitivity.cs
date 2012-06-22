using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class CaseSensitivity
    {
        [Test]
        public void NamesAreCaseSensitive()
        {
            var cA = new TestableContainer( "A", "∋ b" );
            var cB = new TestableContainer( "B" );
            {
                // Starting by CA.
                var r = DependencySorter.OrderItems( cA, cB );
                Assert.That( !r.IsComplete );
                Assert.That( r.HasStructureError && r.StructureErrorCount == 1 );
                ResultChecker.SimpleCheck( r );
            }
        }

    }
}
