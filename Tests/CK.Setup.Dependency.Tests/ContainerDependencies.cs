#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setup.Dependency.Tests\ContainerDependencies.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Dependency.Tests
{
    [TestFixture]
    public class ContainerDependencies
    {
        [Test]
        public void EmptyContainer()
        {
            var c = new TestableContainer( "C" );
            {
                var r = DependencySorter.OrderItems( c );
                r.AssertOrdered( "C.Head", "C" );
                ResultChecker.SimpleCheck( r );
            }
            {
                var r = DependencySorter.OrderItems( true, c );
                r.AssertOrdered( "C.Head", "C" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void AutoChildrenRegistration()
        {
            var c = new TestableContainer( "C", new TestableItem( "A" ), new TestableItem( "B" ) );

            var r = DependencySorter.OrderItems( c );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 4 ) );

            Assert.That( r.SortedItems[0].IsGroupHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "C" ), "Container" );
            new ResultChecker( r ).CheckRecurse( c.FullName );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "C", "A,B" );
        }

        [Test]
        public void AutoContainerRegistration()
        {
            var c = new TestableContainer( "ZeContainer", new TestableItem( "A" ), new TestableItem( "B" ) );
            var e = new TestableItem( "E" );
            c.Add( e );

            var r = DependencySorter.OrderItems( e );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 5 ) );

            Assert.That( r.SortedItems[0].IsGroupHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "E" ), "Lexical order." );
            Assert.That( r.SortedItems[4].Item.FullName, Is.EqualTo( "ZeContainer" ), "Container" );

            new ResultChecker( r ).CheckRecurse( c.FullName, e.FullName );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "ZeContainer", "A,B,E" );
        }

        [Test]
        public void ThreeDependentContainers()
        {
            var c0 = new TestableContainer( "C0", new TestableItem( "A" ), new TestableItem( "B" ), new TestableItem( "C" ) );
            var c1 = new TestableContainer( "C1", new TestableItem( "X" ), "⇀C0" );
            var c2 = new TestableContainer( "C2", new TestableItem( "Y" ), "⇀C1" );
            {
                var r = DependencySorter.OrderItems( c2, c0, c1 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
            {
                var r = DependencySorter.OrderItems( c0, c1, c2 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
            {
                var r = DependencySorter.OrderItems( c2, c1, c0 );
                r.AssertOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
                ResultChecker.SimpleCheck( r );
                r.CheckChildren( "C0", "A,B,C" );
            }
        }

        [Test]
        public void ItemToContainer()
        {
            var c0 = new TestableContainer( "C0", new TestableItem( "A" ), new TestableItem( "B" ), new TestableItem( "C" ) );
            var c1 = new TestableContainer( "C1", new TestableItem( "X", "⇀C0" ) );
            var c2 = new TestableContainer( "C2", new TestableItem( "Y", "⇀C1" ) );
            var r = DependencySorter.OrderItems( c2, c0, c1 );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            ResultChecker.SimpleCheck( r );
            r.CheckChildren( "C0", "A,B,C" );
            r.CheckChildren( "C1", "X" );
            r.CheckChildren( "C2", "Y" );
        }


        [Test]
        public void MissingDependencies()
        {
            var c = new TestableContainer( "Root", "⇀Direct",
                        new TestableContainer( "Pierre", "↽?Direct", "↽Direct",
                            new TestableItem( "Rubis" )
                            ),
                        new TestableContainer( "Nuage", "⇀?OptDirect", "⇀?OptDirect",
                            new TestableItem( "Cumulus" ),
                            new TestableItem( "Stratus" )
                            )
                );
            {
                var r = DependencySorter.OrderItems( c );
                Assert.That( r.ItemIssues[0].Item.FullName, Is.EqualTo( "Root" ) );
                Assert.That( r.ItemIssues[0].RequiredMissingCount, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.First(), Is.EqualTo( "Direct" ) );

                Assert.That( r.ItemIssues[1].Item.FullName, Is.EqualTo( "Nuage" ) );
                Assert.That( r.ItemIssues[1].RequiredMissingCount, Is.EqualTo( 0 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.First(), Is.EqualTo( "?OptDirect" ) );

                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void CycleDetection0()
        {
            var c = new TestableContainer( "A", "⇀ A" );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ A ⇀ A" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection1()
        {
            var c = new TestableContainer( "Root",
                        new TestableContainer( "Pierre", "⇀Stratus",
                            new TestableItem( "Rubis" )
                            ),
                        new TestableContainer( "Nuage", "⇀Pierre",
                            new TestableItem( "Cumulus" ),
                            new TestableItem( "Stratus" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // The detected cycle depends on the algorithm. 
            // This works here because since we register the Root, we the last registered child is Nuage: we know
            // that the cycle starts (and ends) with Nuage because children are in linked list (added at the head).
            // (This remarks is valid for the other CycleDetection below.)
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⇀ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection2()
        {
            var c = new TestableContainer( "Root",
                        new TestableContainer( "Pierre",
                            new TestableItem( "Rubis", "⇀Stratus" )
                            ),
                        new TestableContainer( "Nuage", "⇀Pierre",
                            new TestableItem( "Cumulus" ),
                            new TestableItem( "Stratus" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⊐ Rubis ⇀ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void CycleDetection3()
        {
            var c = new TestableContainer( "Root",
                        new TestableContainer( "Pierre",
                            new TestableItem( "Rubis" )
                            ),
                        new TestableContainer( "Nuage", "⇀Pierre",
                            new TestableItem( "Cumulus" ),
                            new TestableItem( "Stratus", "↽ Rubis" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            // Here we can see the RequiredByRequires relation: ⇌
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Nuage ⇀ Pierre ⊐ Rubis ⇌ Stratus ⊏ Nuage" ) );
            ResultChecker.SimpleCheck( r );
        }

        [Test]
        public void Wahoo()
        {
            var c = new TestableContainer( "Root",
                new TestableItem( "A", "⇀B" ),
                new TestableItem( "B" ),
                new TestableContainer( "G1",
                    new TestableItem( "C", "⇀ AMissingDependency", "⇀E" ),
                    new TestableContainer( "G1.0", "⇀ A",
                        new TestableItem( "NeedInZ", "⇀InsideZ" )
                        )
                    ),
                new TestableContainer( "Z", "⇀E",
                    new TestableItem( "InsideZ", "⇀C", "⇀ ?OptionalMissingDep" )
                    ),
                new TestableItem( "E", "⇀B" ),
                new TestableContainer( "Pierre",
                    new TestableItem( "Rubis" )
                    ),
                new TestableContainer( "Nuage", "⇀Pierre",
                    new TestableItem( "Cumulus", "↽ RequiredByAreIgnoredIfMissing", "↽ ?IfMarkedAsOptinalTheyContinueToBeIgnored" ),
                    new TestableItem( "Stratus" )
                    )
                );
            {
                var r = DependencySorter.OrderItems( c );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Ordering handles duplicates.
                var r = DependencySorter.OrderItems( new IDependentItem[]{ c, c } );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
            {
                // Ordering handles duplicates.
                var r = DependencySorter.OrderItems( new IDependentItem[] { c, c }.Concat( c.Children.Cast<IDependentItem>() ).Concat( c.Children.Cast<IDependentItem>() ), null );
                Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
                new ResultChecker( r ).CheckRecurse( "Root" );
                ResultChecker.SimpleCheck( r );
            }
        }

        [Test]
        public void SimpleGraph()
        {
            var pAB = new TestableContainer( "PackageForAB" );
            var oA = new TestableItem( "A" );
            oA.Container = pAB;
            var oB = new TestableItem( "B" );
            oB.Container = pAB;
            oB.Requires.Add( oA );
            var pABLevel1 = new TestableContainer( "PackageForABLevel1" );
            pABLevel1.Requires.Add( pAB );
            var oBLevel1 = new TestableItem( "ObjectBLevel1" );
            oBLevel1.Container = pABLevel1;
            oBLevel1.Requires.Add( oA );

            var r = DependencySorter.OrderItems( pAB, oA, oB, pABLevel1, oBLevel1 );
            Assert.That( r.IsComplete );
            r.AssertOrdered( "PackageForAB.Head", "A", "B", "PackageForAB", "PackageForABLevel1.Head", "ObjectBLevel1", "PackageForABLevel1" );
            r.CheckChildren( "PackageForAB", "A,B" );
            r.CheckChildren( "PackageForABLevel1", "ObjectBLevel1" );
        }
    }
}
