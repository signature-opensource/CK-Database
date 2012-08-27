using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class ContainerDependenciesAsk
    {
        [Test]
        public void EmptyContainer()
        {
            var c = new TestableContainer( "C" ) { ThisIsNotAContainer = true };
            {
                var r = DependencySorter.OrderItems( c );
                r.AssertOrdered( "C" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void AutoChildrenRegistration()
        {
            var c = new TestableContainer( "C", new TestableItem( "A" ), new TestableItem( "B" ) ) { ThisIsNotAContainer = true };

            var r = DependencySorter.OrderItems( c );
            Assert.That( r.HasStructureError );
            r.LogError( TestHelper.Logger );
        }

        [Test]
        public void AutoContainerRegistration()
        {
            var c = new TestableContainer( "ZeContainer", new TestableItem( "A" ), new TestableItem( "B" ) ) { ThisIsNotAContainer = true };
            var e = new TestableItem( "E" );
            c.Add( e );

            var r = DependencySorter.OrderItems( e );
            Assert.That( r.HasStructureError );
            r.LogError( TestHelper.Logger );
        }

        [Test]
        public void ThreeContainersByName()
        {
            {
                var c0 = new TestableContainer( "C0" ) { ThisIsNotAContainer = true };
                var c1 = new TestableContainer( "C1", "∈C0" );
                var c2 = new TestableContainer( "C2", "∈C1" );
                var r = DependencySorter.OrderItems( c2, c0, c1 );
                r.LogError( TestHelper.Logger );
                Assert.That( r.HasStructureError );
            }
            {
                var c0 = new TestableContainer( "C0" );
                var c1 = new TestableContainer( "C1", "∈C0" ) { ThisIsNotAContainer = true };
                var c2 = new TestableContainer( "C2", "∈C1" );
                var r = DependencySorter.OrderItems( c2, c0, c1 );
                r.LogError( TestHelper.Logger );
                Assert.That( r.HasStructureError );
            }
            {
                var c0 = new TestableContainer( "C0" );
                var c1 = new TestableContainer( "C1", "∈C0" );
                var c2 = new TestableContainer( "C2", "∈C1" ) { ThisIsNotAContainer = true };
                var r = DependencySorter.OrderItems( c2, c0, c1 );
                Assert.That( r.HasStructureError, Is.False, "Success since c2 has no items." );
            }
        }
    }
}
