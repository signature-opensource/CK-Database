using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class FlatDependencies
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void NoItem()
        {
            DependencySorterResult r = DependencySorter.OrderItems( ReadOnlyListEmpty<Item>.Empty, null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems, Is.Empty );
        }

        [Test]
        public void OneItem()
        {
            var oneItem = new Item( "Test" );
            DependencySorterResult r = DependencySorter.OrderItems( new ReadOnlyListMono<Item>( oneItem ), null );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 1 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( oneItem ) );
            new ResultChecker( r ).CheckRecurse( "Test" );
        }

        [Test]
        public void OneItemMissingDependency()
        {
            var oneItem = new Item( "Test", "=>MissingDep" );
            DependencySorterResult r = DependencySorter.OrderItems( new ReadOnlyListMono<Item>( oneItem ), null );
            Assert.That( r.CycleDetected == null );

            Assert.That( r.ItemIssues.Count, Is.EqualTo( 1 ) );
            Assert.That( r.ItemIssues[0].Item, Is.SameAs( oneItem ) );
            Assert.That( r.ItemIssues.SelectMany( m => m.MissingDependencies ), Is.EquivalentTo( new []{ "MissingDep" } ) );
            ResultChecker.CheckMissingInvariants( r );

            Assert.That( r.SortedItems.Count, Is.EqualTo( 1 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( oneItem ) );
            
            new ResultChecker( r ).CheckRecurse( "Test" );
        }

        [Test]
        public void TwoDependencies()
        {
            var i1 = new Item( "Base" );
            var i2 = new Item( "User", "=>Base" );
            {
                DependencySorterResult r = DependencySorter.OrderItems( i1, i2 );
                Assert.That( r.CycleDetected == null );
                Assert.That( r.ItemIssues, Is.Empty );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
                Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
                Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );

                new ResultChecker( r ).CheckRecurse( "Base", "User" );
            }
            {
                // Allowing duplicates (and reversing initial order).
                DependencySorterResult r = DependencySorter.OrderItems( i2, i1, i1, i2 );
                Assert.That( r.CycleDetected == null );
                Assert.That( r.ItemIssues, Is.Empty );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 2 ) );
                Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
                Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );
                
                new ResultChecker( r ).CheckRecurse( "Base", "User" );
            }
        }

        [Test]
        public void DuplicateItemName()
        {
            var i1 = new Item( "Test" );
            var i2 = new Item( "Test" );
            DependencySorterResult r = DependencySorter.OrderItems( i1, i2 );
            Assert.That( r.HasStructureError );
           // Since we start with i1:
            Assert.That( r.ItemIssues[0].Item, Is.SameAs( i1 ) );
            Assert.That( r.ItemIssues[0].Homonyms, Is.EquivalentTo( new []{ i2 } ) );
        }

        [Test]
        public void FiveFullyDefined()
        {
            var i1 = new Item( "System" );
            var i2 = new Item( "Res", "=>System" );
            var i3 = new Item( "Actor", "=>Res" );
            var i4 = new Item( "MCulture", "=>Res", "=>Actor" );
            var i5 = new Item( "Appli", "=>MCulture", "=>Actor" );
            
            var r = DependencySorter.OrderItems( i5, i1, i4, i2, i3 );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 5 ) );
            Assert.That( r.SortedItems[0].Item, Is.SameAs( i1 ) );
            Assert.That( r.SortedItems[1].Item, Is.SameAs( i2 ) );
            Assert.That( r.SortedItems[2].Item, Is.SameAs( i3 ) );
            Assert.That( r.SortedItems[3].Item, Is.SameAs( i4 ) );
            Assert.That( r.SortedItems[4].Item, Is.SameAs( i5 ) );

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "MCulture", "Appli" );
        }

        [Test]
        public void OrderingByNames()
        {
            var i1 = new Item( "System" );
            var i2 = new Item( "Res", "=>System" );
            var i3 = new Item( "Actor", "=>Res" );
            var i3Bis = new Item( "Acto", "=>Res" );
            var i3Ter = new Item( "Act", "=>Res" );
            var i4 = new Item( "MCulture", "=>Res", "=>Actor" );
            var i5 = new Item( "Appli", "=>MCulture", "=>Actor" );
            var i2Like = new Item( "JustLikeRes", "=>System" );
            
            var r = DependencySorter.OrderItems( i5, i2Like, i1, i3Ter, i4, i2, i3Bis, i3 );
            Assert.That( r.CycleDetected == null );
            Assert.That( r.ItemIssues, Is.Empty );
            Assert.That( r.SortedItems.Count, Is.EqualTo( 8 ) );

            Assert.That( r.IsOrdered( "System", "JustLikeRes", "Res", "Act", "Acto", "Actor", "MCulture", "Appli" ),
                "Ordering is deterministic: when 2 dependencies are on the same rank, their lexical order makes the difference." );

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "Acto", "Act", "MCulture", "Appli", "JustLikeRes" );
        }

        [Test]
        public void OrderingByNamesReverse()
        {
            var i1 = new Item( "System" );
            var i2 = new Item( "Res", "=>System" );
            var i3 = new Item( "Actor", "=>Res" );
            var i3Bis = new Item( "Acto", "=>Res", "=>AnAwfulMissingDependency" );
            var i3Ter = new Item( "Act", "=>Res" );
            var i4 = new Item( "MCulture", "=>Res", "=>Actor" );
            var i5 = new Item( "Appli", "=>MCulture", "=>Actor", "=>AnOtherMissingDependency" );
            var i2Like = new Item( "JustLikeRes", "=>System", "=>AnAwfulMissingDependency" );
            
            // Reversing lexical ordering is the last (optional) parameter.
            var r = DependencySorter.OrderItems( true, i5, i2Like, i1, i3Ter, i4, i2, i3Bis, i3 );
            Assert.That( r.CycleDetected == null );

            // Since we started to add i5, the i5 => AnOtherMissingDependency is the first one, then comes the i2Like and the i3Bis.
            Assert.That( r.ItemIssues.SelectMany( d => d.MissingDependencies ), Is.EquivalentTo( new[] { "AnOtherMissingDependency", "AnAwfulMissingDependency", "AnAwfulMissingDependency" } ) );
            ResultChecker.CheckMissingInvariants( r );
            
            Assert.That( r.SortedItems.Count, Is.EqualTo( 8 ) );
            Assert.That( r.IsOrdered( "System", "Res", "JustLikeRes", "Actor", "Acto", "Act", "MCulture", "Appli" ), 
                    @"Reversing of the order for 2 dependencies are on the same rank can help detect missing dependencies: 
                      a setup MUST work regardless of fact that we invert the order of items that have the same rank: since they 
                      share their rank there is NO dependency between them." );

            new ResultChecker( r ).CheckRecurse( "System", "Res", "Actor", "Acto", "Act", "MCulture", "Appli", "JustLikeRes" );
        }

        [Test]
        public void CycleDetection()
        {
            // A => B => C => D => E => F => C
            var a = new Item( "A", "=>B" );
            var b = new Item( "B", "=>C" );
            var c = new Item( "C", "=>D" );
            var d = new Item( "D", "=>E" );
            var e = new Item( "E", "=>F" );
            var f = new Item( "F", "=>C" );
            var r = DependencySorter.OrderItems( e, b, c, d, f, a );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0], Is.SameAs( r.CycleDetected.Last() ), "Detected cycle shares its first and last item." );           
            Assert.That( r.CycleDetected.Skip(1), Is.EquivalentTo( new[] { c, d, e, f } ), "Cycle is detected in its entirety: the 'head' can be any participant." );
        }

        [Test]
        public void CycleDetectionAutoReference()
        {
            // A => B => C => C,D
            var a = new Item( "A", "=>B" );
            var b = new Item( "B", "=>C" );
            var c = new Item( "C", "=>C", "D" );
            var d = new Item( "D" );
            var r = DependencySorter.OrderItems( b, c, d, a );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0], Is.SameAs( r.CycleDetected.Last() ), "Detected cycle shares its first and last item: this is always true (even if there is only one participant)." );
            Assert.That( r.CycleDetected.Count, Is.EqualTo( 2 ), "Cycle is 'c=>c'" );
            Assert.That( r.CycleDetected[0], Is.SameAs( c ), "The culprit is actually the only item." );
        }


        [Test]
        public void RequiredBy()
        {
            // a
            // b d 
            // c f h i
            // e
            // g
            var a = new Item( "A" );
            var b = new Item( "B", "=>A" );
            var c = new Item( "C", "=>B" );
            var d = new Item( "D", "=>A" );
            var e = new Item( "E", "=>C" );
            var f = new Item( "F", "=>B" );
            var g = new Item( "G", "=>E" );
            var h = new Item( "H", "=>B" );
            var i = new Item( "I", "=>D" );
            
            var r = DependencySorter.OrderItems( e, g, b, h, c, d, i, f, a );
            Assert.That( r.IsOrdered( "A", "B", "D", "C", "F", "H", "I", "E", "G" ) );

            // Now, makes D requires E: D => A,E=>(C=>(B=>(A))) (5), where G => E=>(C=>(B=>(A))) (4)
            // G & D have no dependencies between them and actually share the same rank: the lexical order applies.
            // The last one will be I since I => D
            // a
            // b  
            // c f h
            // e
            // g
            // d
            // i
            e.Add( "<= D" );
            r = DependencySorter.OrderItems( e, c, b, g, h, i, d, f, a );
            Assert.That( r.IsOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" ) );

            // This does not change the dependency order per se (it just contributes to make D "heavier" but do not change its rank).
            h.Add( "<= D" );
            r = DependencySorter.OrderItems( f, i, b, g, h, d, e, a, c );
            Assert.That( r.IsOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" ) );

            // Missing "RequiredBy" are just useless: we simply forget them (and they do not change anything in the ordering of course).
            // We do not consider them as "Missing Dependencies" since they are NOT missing dependencies :-).
            a.Add( "<=KExistePas", "<=DuTout" );
            b.Add( "<= KExistePas" );
            r = DependencySorter.OrderItems( f, b, h, i, e, g, a, d, c );
            Assert.That( r.IsOrdered( "A", "B", "C", "F", "H", "E", "D", "G", "I" ) );
            Assert.That( r.ItemIssues, Is.Empty );

            // Of course, RequiredBy participates to cycle...
            // B => D => (E, H => B) => C => (B, H => B)
            // Here we created 3 cycles: 
            //  - B => D => H => B
            //  - B => D => E => C => B
            //  - B => D => E => C => H => B
            Assert.That( d.RequiredBy, Is.Null.Or.Empty, "Otherwise this test will fail :-)." );
            d.Add( "<=B" );
            r = DependencySorter.OrderItems( f, b, h, i, e, g, a, d, c );
            Assert.That( r.SortedItems, Is.Null );
            Assert.That( r.CycleDetected[0], Is.SameAs( r.CycleDetected.Last() ), "Detected cycle shares its first and last item." );

            bool cycle1 = Is.EquivalentTo( new[] { b, d, h } ).Matches( r.CycleDetected.Skip( 1 ) );
            bool cycle2 = Is.EquivalentTo( new[] { b, d, e, c } ).Matches( r.CycleDetected.Skip( 1 ) );
            bool cycle3 = Is.EquivalentTo( new[] { b, d, e, c, h } ).Matches( r.CycleDetected.Skip( 1 ) );
            Assert.That( cycle1 || cycle2 || cycle3 );
        }


        [Test]
        public void RelatedItems()
        {
            var i1 = new Item( "I1" );
            var i2 = new Item( "I2" );
            var i3 = new Item( "I3" );
            // Auto reference:
            i1.RelatedItems.Add( i1 );
            i1.RelatedItems.Add( i2 );
            i1.RelatedItems.Add( i3 );

            var i4 = new Item( "I4" );
            var i5 = new Item( "I5" );
            var i6 = new Item( "I6" );

            i2.RelatedItems.Add( i4 );
            i4.RelatedItems.Add( i5 );
            i5.RelatedItems.Add( i6 );
            // Back to i2.
            i6.RelatedItems.Add( i2 );

            {
                var r = DependencySorter.OrderItems( i1 );
                Assert.That( r.HasStructureError, Is.False );
                Assert.That( r.IsComplete, Is.True );
                Assert.That( r.SortedItems.Count, Is.EqualTo( 6 ) );
            }

        }
    }
}
