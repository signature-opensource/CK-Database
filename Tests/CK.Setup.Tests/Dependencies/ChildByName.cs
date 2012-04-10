using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class ChildByName
    {
        [Test]
        public void JustContainers()
        {
            var cB = new ItemContainer( "CB", "∋ CA" );
            var cA = new ItemContainer( "CA" );
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
            var cB = new ItemContainer( "CB", "∋ OB" );
            var oB = new Item( "OB" );
            {
                // Starting with the Container.
                var r = DependencySorter.OrderItems( cB, oB );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "OB", "CB" ) );
            }
            {
                // Starting with the Item.
                var r = DependencySorter.OrderItems( oB, cB );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "OB", "CB" ) );
            }
            var cA = new ItemContainer( "CA", "∋ OA" );
            cB.Add( "∋ CA" );
            var oA = new Item( "OA" );
            {
                // Starting with the Containers.
                var r = DependencySorter.OrderItems( cB, oB, cA, oA );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" ) );
            }
            {
                // Starting with the Items.
                var r = DependencySorter.OrderItems( oB, oA, cB, cA );
                Assert.That( r.IsComplete );
                Assert.That( r.IsOrdered( "CB.Head", "CA.Head", "OB", "OA", "CA", "CB" ) );
            }
        }

        [Test]
        public void MissingChild()
        {
            var cB = new ItemContainer( "CB", "∋ CA" );
            {
                var r = DependencySorter.OrderItems( cB );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.IsOrdered( "CB.Head", "CB" ) );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild ) );
                Assert.That( r.ItemIssues[0].MissingChildren.Single(), Is.EqualTo( "CA" ) );
            }
        }

        [Test]
        public void ExtraneousContainer()
        {
            var childOfCB2 = new Item( "ChildOfCB2" );
            var cB1 = new ItemContainer( "CB1", "∋ ChildOfCB2" );
            var cB2 = new ItemContainer( "CB2", childOfCB2 );
            {
                var r = DependencySorter.OrderItems( cB1, cB2 );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.IsOrdered( "CB1.Head", "CB2.Head", "CB1", "ChildOfCB2", "CB2" ) );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );

                var issue = r.ItemIssues.Single( i => i.Item == childOfCB2 );
                Assert.That( issue.StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( issue.ExtraneousContainers.Single(), Is.EqualTo( "CB1" ) );
            }
        }


        [Test]
        public void MultipleStructureErrors()
        {
            var childOfCB2 = new Item( "ChildOfCB2" );
            var cB1 = new ItemContainer( "CB1", "∋ MissingChild", "∋ ChildOfCB2" );
            var cB2 = new ItemContainer( "CB2", "∋ MissingChild", "∈ MissingContainer", "=> MissingDependency", childOfCB2 );
            var cB3 = new ItemContainer( "CB3", "∈ ChildOfCB2", "=> MissingDependency" );
            // This "discovers" an homonym.
            cB3.RelatedItems.Add( new Item( "CB1" ) );
            {
                var r = DependencySorter.OrderItems( cB1, cB2, cB3 );
                Assert.That( r.IsComplete, Is.False );
                Assert.That( r.IsOrdered( "CB1.Head", "CB2.Head", "CB3.Head", "CB1", "CB3", "ChildOfCB2", "CB2" ) );
                Assert.That( r.HasStructureError, Is.True );
                Assert.That( r.ItemIssues.Count, Is.EqualTo( 4 ) );
                
                var issue1 = r.ItemIssues.Single( i => i.Item == cB1 );
                Assert.That( issue1.StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild | DependentItemStructureError.Homonym ) );
                Assert.That( issue1.MissingChildren.Single(), Is.EqualTo( "MissingChild" ) );
                Assert.That( issue1.Homonyms.Single().FullName, Is.EqualTo( "CB1" ) );
                
                var issue2 = r.ItemIssues.Single( i => i.Item == cB2 );
                Assert.That( issue2.StructureError, Is.EqualTo( DependentItemStructureError.MissingNamedChild | DependentItemStructureError.MissingNamedContainer | DependentItemStructureError.MissingDependency ) );
                Assert.That( issue2.MissingChildren.Single(), Is.EqualTo( "MissingChild" ) );
                Assert.That( issue2.MissingDependencies.Single(), Is.EqualTo( "MissingDependency" ) );

                var issue3 = r.ItemIssues.Single( i => i.Item == cB3 );
                Assert.That( issue3.StructureError, Is.EqualTo( DependentItemStructureError.ExistingItemIsNotAContainer | DependentItemStructureError.MissingDependency ) );
                Assert.That( issue3.MissingDependencies.Single(), Is.EqualTo( "MissingDependency" ) );

                var issue4 = r.ItemIssues.Single( i => i.Item == childOfCB2 );
                Assert.That( issue4.StructureError, Is.EqualTo( DependentItemStructureError.MultipleContainer ) );
                Assert.That( issue4.ExtraneousContainers.Single(), Is.EqualTo( "CB1" ) );

            }
        }

    }
}
