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
            var cB = new ItemContainer( "CB" );
            var cA = new ItemContainer( "CA", "∈ CB" );
            {
                // Starting by CA.
                var r = DependencySorter.OrderItems( cA, cB );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "CA", "CB" ) );
            }
            {
                // Starting by CB.
                var r = DependencySorter.OrderItems( cB, cA );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "CA", "CB" ) );
            }
        }

        [Test]
        public void SomeItems()
        {
            var c = new ItemContainer( "C" );
            var o1 = new Item( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.IsOrdered( "C.Head", "O1", "C" ) );
            }
            var o2 = new Item( "O2", "∈ O1" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2 );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
            }
            o2.Add( "∈ C", "<= O1" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2 );
                Assert.That( r.IsOrdered( "C.Head", "O2", "O1", "C" ) );
            }
            var sub = new Item( "Cycle", "∈ C", "=> C" );
            {
                var r = DependencySorter.OrderItems( c, o1, o2, sub );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∋ Cycle ⇒ C" ) );
            }
        }

        [Test]
        public void MissingContainer()
        {
            var o = new Item( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( o );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.IsOrdered( "O1" ) );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedContainer ) );
                Assert.That( r.ItemIssues[0].MissingChildren, Is.Empty );
            }
        }

        [Test]
        public void MonoCycle()
        {
            var c = new ItemContainer( "C", "=> C" );
            var o1 = new Item( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ⇒ C" ) );
            }
        }

        [Test]
        public void AutoContains()
        {
            var c = new ItemContainer( "C", "∈ C" );
            var o1 = new Item( "O1", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1 );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∈ C" ) );
            }
        }


        [Test]
        public void RecurseAutoContains()
        {
            var c = new ItemContainer( "C", "∈ D" );
            var o1 = new Item( "O1", "∈ C" );
            var d = new ItemContainer( "D", "∈ C" );
            {
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ C ∈ D ∈ C" ) );
            }
        }

        [Test]
        public void MultiContainerByName()
        {
            var o1 = new Item( "O1" );
            var c = new ItemContainer( "C", o1 );

            Assert.That( c.Children.Contains( o1 ) && o1.Container == c );
            o1.Add( "∈ D" );
            Assert.That( c.Children.Contains( o1 ) && o1.Container != c && o1.Container.FullName == "D" );

            var d = new ItemContainer( "D" );

            {
                // Starting by C: O1 is discovered by C.Children: the extraneous container is D.
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "D" ) );
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
            }
        }

        [Test]
        public void MultiContainerByref()
        {
            var o1 = new Item( "O1" );
            var c = new ItemContainer( "C", o1 );
            var d = new ItemContainer( "D" );

            Assert.That( c.Children.Contains( o1 ) && o1.Container == c );
            o1.Container = d;
            Assert.That( c.Children.Contains( o1 ) && o1.Container != c && o1.Container.FullName == "D" );


            {
                // Starting by C: O1 is discovered by C.Children: the extraneous container is D.
                var r = DependencySorter.OrderItems( c, o1, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "D" ) );
            }

            {
                // Starting by o1: its container is D, the extraneous container is C.
                var r = DependencySorter.OrderItems( o1, c, d );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( r.ItemIssues[0].ExtraneousContainers.Single(), Is.EqualTo( "C" ) );
            }
        }

        [Test]
        public void WhenTheItemContainerIsNull()
        {
            var o1 = new Item( "O1" );
            var c = new ItemContainer( "C", o1 );

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
            }

            {
                // Starting by O1: its container becomes C.
                var r = DependencySorter.OrderItems( o1, c );
                Assert.That( r.IsComplete, Is.True );
                Assert.That( r.HasStructureError, Is.False );
                Assert.That( r.IsOrdered( "C.Head", "O1", "C" ) );
                Assert.That( r.SortedItems[1].Container.FullName, Is.EqualTo( "C" ) );
            }


        }


    }
}
