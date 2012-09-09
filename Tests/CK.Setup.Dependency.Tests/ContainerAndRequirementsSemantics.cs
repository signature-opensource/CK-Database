using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Setup.Tests;
using CK.Setup.Tests.Dependencies;

namespace CK.Setup.Tests.Dependencies
{
    [TestFixture]
    public class ContainerAndRequirementsSemantics
    {

        [Test]
        public void ContainerSpecializationLoop()
        {
            using( TestableItem.IgnoreCheckCount() )
            {
                var System = new TestableContainer( "System" );

                // A is a core package (User Management) in System.
                var A = new TestableContainer( "A" );
                A.Container = System;
                // It contains B (Login Management).
                var B = new TestableContainer( "B" );
                B.Container = A;
                // For a project, a specialized User Management is needed.
                var ASpec = new TestableContainer( "ASpec" );
                // ASpec specializes A: 
                // - in term of dependencies, it requires A.
                // - since it specializes A, unless explicitely injected into another container it is also in System.
                ASpec.Requires.Add( A );
                ASpec.Container = System;

                {
                    var r = DependencySorter.OrderItems( A, B, ASpec );
                    Assert.That( r.IsComplete );
                    r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "ASpec.Head", "ASpec", "System" );
                }

                // This "Requires me + use my Container" relationship is "Generalization".
                ASpec.Requires.Clear();
                ASpec.Container = null;
                // Previous scenario is the same as:
                ASpec.Generalization = A;
                {
                    var r = DependencySorter.OrderItems( A, B, ASpec );
                    Assert.That( r.IsComplete );
                    r.AssertOrdered( "System.Head", "A.Head", "B.Head", "B", "A", "ASpec.Head", "ASpec", "System" );
                }


                // John would like ASpec to be inside the package A:
                ASpec.Container = A;
                // It does not work: how a more specialized package can be contained in its "base"?
                {
                    var r = DependencySorter.OrderItems( A, B, ASpec );
                    Assert.That( !r.IsComplete );
                    Assert.That( r.CycleDetected, Is.Not.Null );
                }
                // Of course, ASpec can not be inside B...
                ASpec.Container = B;
                {
                    var r = DependencySorter.OrderItems( A, B, ASpec );
                    Assert.That( !r.IsComplete );
                    Assert.That( r.CycleDetected, Is.Not.Null );
                }
            }
        }
    }
}
