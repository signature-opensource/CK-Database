using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace CK.Setup.Dependency.Tests
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

            public object StartDependencySort()
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
        public void GetReferences()
        {
            IDependentItem item = new Item() { FullName = "Item" };
            IDependentItemGroup group = new Group() { FullName = "Group" };
            IDependentItemContainer container = new Container() { FullName = "Container" };
            
            IDependentItemRef refItem = item.GetReference();
            IDependentItemGroupRef refGroup = group.GetReference();
            IDependentItemContainerRef refContainer = container.GetReference();

            Assert.That( refItem.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroup.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainer.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItem.Optional, Is.False );
            Assert.That( refGroup.Optional, Is.False );
            Assert.That( refContainer.Optional, Is.False );

            IDependentItemRef refItemO = refItem.GetOptionalReference();
            IDependentItemGroupRef refGroupO = refGroup.GetOptionalReference();
            IDependentItemContainerRef refContainerO = refContainer.GetOptionalReference();

            Assert.That( refItemO.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroupO.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainerO.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItemO.Optional, Is.True );
            Assert.That( refGroupO.Optional, Is.True );
            Assert.That( refContainerO.Optional, Is.True );

            IDependentItemRef refItem2 = refItemO.GetReference();
            IDependentItemGroupRef refGroup2 = refGroupO.GetReference();
            IDependentItemContainerRef refContainer2 = refContainerO.GetReference();
            Assert.That( refItem2.FullName, Is.EqualTo( "Item" ) );
            Assert.That( refGroup2.FullName, Is.EqualTo( "Group" ) );
            Assert.That( refContainer2.FullName, Is.EqualTo( "Container" ) );
            Assert.That( refItem2.Optional, Is.False );
            Assert.That( refGroup2.Optional, Is.False );
            Assert.That( refContainer2.Optional, Is.False );
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

            var eC = eItem.EnsureContextPrefix( "C" );

            Assert.That( eC.ElementAt( 0 ), Is.SameAs( eItem.ElementAt( 0 ) ), "DependentItem are kept as-is (current implementation)." );
            Assert.That( eC.ElementAt( 1 ), Is.SameAs( eItem.ElementAt( 1 ) ), "DependentItem are kept (current implementation)." );
            Assert.That( eC.ElementAt( 2 ), Is.SameAs( eItem.ElementAt( 2 ) ), "DependentItem are kept (current implementation)." );

            var eNamed = eC.ElementAt( 3 );
            Assert.That( eNamed, Is.Not.SameAs( eItem.ElementAt( 3 ) ), "Named reference are changed." );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]RefItem" ) );
            Assert.That( eNamed, Is.AssignableTo<IDependentItemRef>() );
            eNamed = eC.ElementAt( 4 );
            Assert.That( eNamed, Is.Not.SameAs( eItem.ElementAt( 4 ) ), "Named reference are changed." );
            Assert.That( eNamed.FullName, Is.EqualTo( "[C]RefGroup" ) );
            Assert.That( eNamed, Is.AssignableTo<IDependentItemGroupRef>() );
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
            IEnumerable<IDependentItemGroupRef> eGroupC = eGroup.EnsureContextPrefix( "C" );

            IEnumerable<IDependentItemContainerRef> eContainer = new IDependentItemContainerRef[] { };
            IEnumerable<IDependentItemContainerRef> eContainerC = eContainer.EnsureContextPrefix( "C" );


        }
    }
}
