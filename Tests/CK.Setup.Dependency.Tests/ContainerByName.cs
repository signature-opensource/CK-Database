using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class ContainerByName
    {
        [Test]
        public void JustContainers()
        {
            var cB = new TestableContainer( "CB" );
            var cA = new TestableContainer( "CA", "∈ CB" );
            {
                // Starting by CA.
                var r = DependencySorter.OrderItems( cA, cB );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "CA", "CB" ) );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Starting by CB.
                var r = DependencySorter.OrderItems( cB, cA );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "CA", "CB" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void SomeItems()
        {
            var c = new TestableContainer( "C" );
            var o1 = new TestableItem( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.IsOrdered( "C.Head", "O1", "C" ) );
                ResultChecker.SimpleCheck( r );
            }
            var o2 = new TestableItem( "O2", "∈ O1" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2 );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                ResultChecker.SimpleCheck( r );
            }
            o2.Add( "∈ C", "<= O1" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2 );
                Assert.That( r.IsOrdered( "C.Head", "O2", "O1", "C" ) );
                ResultChecker.SimpleCheck( r );
            }
            var sub = new TestableItem( "Cycle", "∈ C", "=> C" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2, sub );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∋ Cycle ⇒ C" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void MissingContainer()
        {
            var o = new TestableItem( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( o );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.IsOrdered( "O1" ) );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedContainer ) );
                Assert.That( r.ItemIssues[0].MissingChildren, Is.Empty );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void MonoCycle()
        {
            var c = new TestableContainer( "C", "=> C" );
            var o1 = new TestableItem( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ⇒ C" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void AutoContains()
        {
            var c = new TestableContainer( "C", "∈ C" );
            var o1 = new TestableItem( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∈ C" ) );
                ResultChecker.SimpleCheck( r );
            }
        }


        [Test]
        public void RecurseAutoContains()
        {
            var c = new TestableContainer( "C", "∈ D" );
            var o1 = new TestableItem( "O1", "∈ C" );
            var d = new TestableContainer( "D", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∈ D ∈ C" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void MultiContainerByName()
        {
            var o1 = new TestableItem( "O1" );
            var c = new TestableContainer( "C", o1 );

            Assert.That( c.Children.Contains( o1 ) && o1.Container == c );
            o1.Add( "∈ D" );
            Assert.That( c.Children.Contains( o1 ) && o1.Container != c );

            var d = new TestableContainer( "D" );

            {
                // Starting by C: O1 is discovered by C.Children: the extraneous container is D.
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "D" ) );
                ResultChecker.SimpleCheck( r );
            }

            {
                // Starting by o1: its container is still C (the extraneous container is still D)
                // since named containers binding is deferred: c.Children wins one again.
                // Whatever the order is, what is important is that IsComplete is false and a ExtraneousContainers is detected.
                var r = DependencySorter.OrderItems( o1, c, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "D" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void MultiContainerByref()
        {
            var o1 = new TestableItem( "O1" );
            var c = new TestableContainer( "C", o1 );
            var d = new TestableContainer( "D" );

            Assert.That( c.Children.Contains( o1 ) && o1.Container == c );
            o1.Container = d;
            Assert.That( c.Children.Contains( o1 ) && o1.Container != c );


            {
                // Starting by C: O1 is discovered by C.Children: the extraneous container is D.
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "D" ) );
                ResultChecker.SimpleCheck( r );
            }

            {
                // Starting by o1: its container is D, the extraneous container is C.
                var r = DependencySorter.OrderItems( o1, c, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "C" ) );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void WhenTheItemContainerIsNull()
        {
            var o1 = new TestableItem( "O1" );
            var c = new TestableContainer( "C", o1 );

            Assert.That( c.Children.Contains( o1 ) && o1.Container == c );
            o1.Container = null;
            Assert.That( c.Children.Contains( o1 ) && o1.Container == null );

            {
                // Starting by C: O1 is discovered by C.Children: the container becomes C since O1 does not say anything.
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.IsComplete, Is.True );
                Assert.That( r.HasStructureError, Is.False );
                Assert.That( r.IsOrdered( "C.Head", "O1", "C" ) );
                Assert.That( r.SortedItems[1].Container.FullName, Is.EqualTo( "C" ) );
                ResultChecker.SimpleCheck( r );
            }

            {
                // Starting by O1: its container becomes C.
                var r = DependencySorter.OrderItems( o1, c );
                Assert.That( r.IsComplete, Is.True );
                Assert.That( r.HasStructureError, Is.False );
                Assert.That( r.IsOrdered( "C.Head", "O1", "C" ) );
                Assert.That( r.SortedItems[1].Container.FullName, Is.EqualTo( "C" ) );
                ResultChecker.SimpleCheck( r );
            }


        }


    }
}
