using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class ContainerDependencies
    {
        [Test]
        public void EmptyContainer()
        {
            var c = new ItemContainer( "C" );
            {
                var r = DependencySorter.OrderItems( c );
                Assert.That( r.IsOrdered( "C.Head", "C" ) );
            }
            {
                var r = DependencySorter.OrderItems( true, c );
                Assert.That( r.IsOrdered( "C.Head", "C" ) );
            }
        }

        [Test]
        public void AutoChildrenRegistration()
        {
            var c = new ItemContainer( "C", new Item( "A" ), new Item( "B" ) );

            var r = DependencySorter.OrderItems( c );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 4 ) );

            Assert.That( r.SortedItems[0].IsContainerHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "C" ), "Container" );

            new ResultChecker( r ).CheckRecurse( c.FullName );
        }

        [Test]
        public void AutoContainerRegistration()
        {
            var c = new ItemContainer( "ZeContainer", new Item( "A" ), new Item( "B" ) );
            var e = new Item( "E" );
            c.Add( e );

            var r = DependencySorter.OrderItems( e );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 5 ) );

            Assert.That( r.SortedItems[0].IsContainerHead, "Head of Container." );
            Assert.That( r.SortedItems[1].Item.FullName, Is.EqualTo( "A" ), "Lexical order." );
            Assert.That( r.SortedItems[2].Item.FullName, Is.EqualTo( "B" ), "Lexical order." );
            Assert.That( r.SortedItems[3].Item.FullName, Is.EqualTo( "E" ), "Lexical order." );
            Assert.That( r.SortedItems[4].Item.FullName, Is.EqualTo( "ZeContainer" ), "Container" );
            
            new ResultChecker( r ).CheckRecurse( c.FullName, e.FullName );
        }

        [Test]
        public void ThreeDependentContainers()
        {
            var c0 = new ItemContainer( "C0", new Item( "A" ), new Item( "B" ), new Item( "C" ) );
            var c1 = new ItemContainer( "C1", new Item( "X" ), "=>C0" );
            var c2 = new ItemContainer( "C2", new Item( "Y" ), "=>C1" );
            {
                var r = DependencySorter.OrderItems( c2, c0, c1 );
                Assert.That( r.IsOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" ) );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            }
            {
                var r = DependencySorter.OrderItems( c0, c1, c2 );
                Assert.That( r.IsOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" ) );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            }
            {
                var r = DependencySorter.OrderItems( c2, c1, c0 );
                Assert.That( r.IsOrdered( "C0.Head", "A", "B", "C", "C0", "C1.Head", "X", "C1", "C2.Head", "Y", "C2" ) );
                new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
            }
        }

        [Test]
        public void ItemToContainer()
        {
            var c0 = new ItemContainer( "C0", new Item( "A" ), new Item( "B" ), new Item( "C" ) );
            var c1 = new ItemContainer( "C1", new Item( "X", "=>C0" ) );
            var c2 = new ItemContainer( "C2", new Item( "Y", "=>C1" ) );
            var r = DependencySorter.OrderItems( c2, c0, c1 );
            new ResultChecker( r ).CheckRecurse( "C0", "C1", "C2" );
        }


        [Test]
        public void MissingDependencies()
        {
            var c = new ItemContainer( "Root", "=>Direct",
                        new ItemContainer( "Pierre", "<=?Direct", "<=Direct",
                            new Item( "Rubis" )
                            ),
                        new ItemContainer( "Nuage", "=>?OptDirect", "=>?OptDirect",
                            new Item( "Cumulus" ),
                            new Item( "Stratus" )
                            )
                );
            {
                var r = DependencySorter.OrderItems( c );
                ResultChecker.CheckMissingInvariants( r );
                Assert.That( r.ItemIssues[0].Item.FullName, Is.EqualTo( "Root" ) );
                Assert.That( r.ItemIssues[0].RequiredMissingCount, Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[0].MissingDependencies.First(), Is.EqualTo( "Direct" ) );

                Assert.That( r.ItemIssues[1].Item.FullName, Is.EqualTo( "Nuage" ) );
                Assert.That( r.ItemIssues[1].RequiredMissingCount, Is.EqualTo( 0 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.Count(), Is.EqualTo( 1 ) );
                Assert.That( r.ItemIssues[1].MissingDependencies.First(), Is.EqualTo( "?OptDirect" ) );
            }
        }

        [Test]
        public void CycleDetection0()
        {
            var c = new ItemContainer( "A", "=> A" );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ A ⇒ A" ) );
        }

        [Test]
        public void CycleDetection1()
        {
            var c = new ItemContainer( "Root",
                        new ItemContainer( "Pierre", "=>Stratus",
                            new Item( "Rubis" )
                            ),
                        new ItemContainer( "Nuage", "=>Pierre",
                            new Item( "Cumulus" ),
                            new Item( "Stratus" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // The detected cycle depends on the algorithm. 
            // This works here because since we register the Root, we first find its Child Pierre: we know
            // that the cycle starts (and ends) with Pierre.
            // (This remarks is valid for the other CycleDetection below.)
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Pierre ⇒ Stratus ∈ Nuage ⇒ Pierre" ) );
        }

        [Test]
        public void CycleDetection2()
        {
            var c = new ItemContainer( "Root",
                        new ItemContainer( "Pierre",
                            new Item( "Rubis", "=>Stratus" )
                            ),
                        new ItemContainer( "Nuage", "=>Pierre",
                            new Item( "Cumulus" ),
                            new Item( "Stratus" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Pierre ∋ Rubis ⇒ Stratus ∈ Nuage ⇒ Pierre" ) );
        }

        [Test]
        public void CycleDetection3()
        {
            var c = new ItemContainer( "Root",
                        new ItemContainer( "Pierre",
                            new Item( "Rubis" )
                            ),
                        new ItemContainer( "Nuage", "=>Pierre",
                            new Item( "Cumulus" ),
                            new Item( "Stratus", "<= Rubis" )
                            )
                );
            var r = DependencySorter.OrderItems( c );
            Assert.That( r.CycleDetected, Is.Not.Null );
            Assert.That( r.SortedItems, Is.Null );
            // See remark in CycleDetection1.
            // Here we can see the Required By relation: ⇆
            Assert.That( r.CycleExplainedString, Is.EqualTo( "↳ Pierre ∋ Rubis ⇆ Stratus ∈ Nuage ⇒ Pierre" ) );
        }

        [Test]
        public void Wahoo()
        {
            var c = new ItemContainer( "Root",
                new Item( "A", "=>B" ),
                new Item( "B" ),
                new ItemContainer( "G1", 
                    new Item( "C", "=> AMissingDependency", "=>E" ),
                    new ItemContainer( "G1.0", "=> A",
                        new Item( "NeedInZ", "=>InsideZ" )
                        )
                    ),
                new ItemContainer( "Z", "=>E",
                    new Item( "InsideZ", "=>C", "=> ?OptionalMissingDep" )
                    ),
                new Item( "E", "=>B"),
                new ItemContainer( "Pierre", 
                    new Item( "Rubis" )
                    ),
                new ItemContainer( "Nuage", "=>Pierre",
                    new Item( "Cumulus", "<= RequiredByAreIgnoredIfMissing", "<= ?IfMarkedAsOptinalTheyContinueToBeIgnored" ),
                    new Item( "Stratus" )
                    )
                );
            var r = DependencySorter.OrderItems( c );
            
            Assert.That( r.ItemIssues.Any( m => m.MissingDependencies.Contains( "AMissingDependency" ) ) );
            ResultChecker.CheckMissingInvariants( r );

            new ResultChecker( r ).CheckRecurse( "Root" );
        }
    }
}
