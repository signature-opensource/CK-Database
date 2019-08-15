#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.Setupable.Engine.Tests\ItemAndItemRef.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using CK.Core;
using CK.Setup;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class ItemAndItemRef
    {
        class Item : IDependentItem, IDependentItemRef
        {
            public string FullName { get; set; }

            public bool Optional { get; set; }

            public IDependentItemContainerRef Container
            {
                get { throw new NotImplementedException(); }
            }

            public IDependentItemRef Generalization
            {
                get { throw new NotImplementedException(); }
            }

            public IEnumerable<IDependentItemRef> Requires
            {
                get { throw new NotImplementedException(); }
            }

            public IEnumerable<IDependentItemRef> RequiredBy
            {
                get { throw new NotImplementedException(); }
            }

            public IEnumerable<IDependentItemGroupRef> Groups
            {
                get { throw new NotImplementedException(); }
            }

            public object StartDependencySort( IActivityMonitor m )
            {
                throw new NotImplementedException();
            }

        }

        class Group : Item, IDependentItemGroup, IDependentItemGroupRef
        {
            public IEnumerable<IDependentItemRef> Children
            {
                get { throw new NotImplementedException(); }
            }
        }

        class Container : Group, IDependentItemContainer, IDependentItemContainerRef
        {
        }

        [Test]
        public void EnsureContext()
        {
            IEnumerable<IDependentItemRef> eItem = new IDependentItemRef[] 
            { 
                new Item() { FullName = "Item" }, new Group() { FullName = "Group" }, new Container() { FullName = "Container" },
                new NamedDependentItemRef( "RefItem" ), new NamedDependentItemGroupRef( "RefGroup" ), new NamedDependentItemContainerRef( "RefContainer" ),
                new NamedDependentItemRef( "[Already]RefItem" ), new NamedDependentItemGroupRef( "[Already]RefGroup" ), new NamedDependentItemContainerRef( "[Already]RefContainer" ) 
            };
            var eC = eItem.SetRefFullName( e => DefaultContextLocNaming.Resolve( e.FullName, "C", null ) );

            Assert.That( eC.ElementAt( 0 ), Is.SameAs( eItem.ElementAt( 0 ) ), "DependentItem are kept as-is (current implementation)." );
            Assert.That( eC.ElementAt( 1 ), Is.SameAs( eItem.ElementAt( 1 ) ), "DependentItem are kept (current implementation)." );
            Assert.That( eC.ElementAt( 2 ), Is.SameAs( eItem.ElementAt( 2 ) ), "DependentItem are kept (current implementation)." );

            var eNamed = eC.ElementAt( 3 );
            Assert.That( eNamed, Is.Not.SameAs( eItem.ElementAt( 3 ) ), "Named reference are changed." );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]RefItem" ) );
            Assert.That( eNamed, Is.AssignableTo<IDependentItemRef>(), "Types are respected." );
            Assert.That( eNamed, Is.Not.AssignableTo<IDependentItemGroupRef>(), "Types are respected." );
            eNamed = eC.ElementAt( 4 );
            Assert.That( eNamed, Is.Not.SameAs( eItem.ElementAt( 4 ) ), "Named reference are changed." );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]RefGroup" ) );
            Assert.That( eNamed, Is.AssignableTo<IDependentItemGroupRef>() );
            Assert.That( eNamed, Is.Not.AssignableTo<IDependentItemContainerRef>(), "Types are respected." );
            eNamed = eC.ElementAt( 5 );
            Assert.That( eNamed, Is.Not.SameAs( eItem.ElementAt( 5 ) ), "Named reference are changed." );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]RefContainer" ) );
            Assert.That( eNamed, Is.AssignableTo<IDependentItemContainerRef>() );

            var eNamedUnchanged = eC.ElementAt( 6 );
            Assert.That( eNamedUnchanged.FullName, Is.EqualTo( "[Already]RefItem" ) );
            eNamedUnchanged = eC.ElementAt( 7 );
            Assert.That( eNamedUnchanged.FullName, Is.EqualTo( "[Already]RefGroup" ) );

            // Overload resolution kindly support specialized versions.
            IEnumerable<IDependentItemGroupRef> eGroup = new IDependentItemGroupRef[] { };
            IEnumerable<IDependentItemGroupRef> eGroupC = eGroup.SetRefFullName( e => DefaultContextLocNaming.Resolve( e.FullName, "Ctx", "Loc" ) );

            IEnumerable<IDependentItemContainerRef> eContainer = new IDependentItemContainerRef[] { };
            IEnumerable<IDependentItemContainerRef> eContainerC = eContainer.SetRefFullName( e => DefaultContextLocNaming.Resolve( e.FullName, "Ctx", "Loc" ) );


        }

        [Test]
        public void EnsureContextAndLocation()
        {
            IEnumerable<IDependentItemRef> eItem = new IDependentItemRef[] 
            { 
                new NamedDependentItemRef( "RefItem" ), new NamedDependentItemGroupRef( "[Already]RefGroup" ), new NamedDependentItemContainerRef( "Where^RefContainer" )
            };
            var eC = eItem.SetRefFullName( e => DefaultContextLocNaming.Resolve( e.FullName, "C", "Loc" ) );

            var eNamed = eC.ElementAt( 0 );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]Loc^RefItem" ) );
            eNamed = eC.ElementAt( 1 );
            Assert.That( eNamed.FullName, Is.EqualTo( "[Already]Loc^RefGroup" ) );
            eNamed = eC.ElementAt( 2 );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]Where^RefContainer" ) );
        }
    }
}
