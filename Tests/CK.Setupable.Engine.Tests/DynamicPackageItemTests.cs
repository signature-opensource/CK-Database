using System.Linq;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;

using static CK.Testing.MonitorTestHelper;

namespace CK.Setupable.Engine.Tests
{
    [TestFixture]
    public class DynamicPackageItemTests
    {
        [Test]
        public void Model_and_ObjectsPackage_are_correctly_ordered()
        {
            var pA = new DynamicPackageItem( "Package" ) { Name = "A" };
            var pB = new DynamicPackageItem( "Package" ) { Name = "B" };
            var pAObjects = pA.EnsureObjectsPackage();
            var pBObjects = pB.EnsureObjectsPackage();
            var pAModel = pA.EnsureModelPackage();
            var pBModel = pB.EnsureModelPackage();
            pB.Requires.Add( pA );
            var sortResult = DependencySorter.OrderItems( TestHelper.Monitor, pB );
            Assert.That( sortResult.IsComplete );
            var sortedNames = sortResult.SortedItems.Select( i => i.FullName ).ToArray();
            sortedNames.Should().BeEquivalentTo( new[]
            {
                "Model.A.Head",
                "Model.A",
                "A.Head",
                "Model.B.Head",
                "A",
                "Model.B",
                "B.Head",
                "Objects.A.Head",
                "B",
                "Objects.A",
                "Objects.B.Head",
                "Objects.B"
            }, o => o.WithStrictOrdering() );
        }
    }
}
