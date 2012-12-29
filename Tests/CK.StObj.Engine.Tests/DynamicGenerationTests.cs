using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.Setup;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class DynamicGenerationTests
    {

        class AutoImplementedAttribute : Attribute, IAutoImplementorMethod
        {
            public bool Implement( IActivityLogger logger, System.Reflection.MethodInfo m, System.Reflection.Emit.TypeBuilder b, bool isVirtual )
            {
                CK.Reflection.EmitHelper.ImplementEmptyStubMethod( b, m, isVirtual );
                return true;
            }
        }

        public class A : IAmbientContract
        {
        }

        public abstract class B : A
        {
            [AutoImplemented]
            public abstract int Auto( int i );
        }

        public class C : IAmbientContract
        {
            [AmbientContract]
            public A TheA { get; private set; }
        }

        [Test]
        public void SimpleEmit()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterClass( typeof( B ) );
            collector.RegisterClass( typeof( C ) );
            collector.DependencySorterHookInput = TestHelper.Trace;
            collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
            var r = collector.GetResult();
            Assert.That( r.HasFatalError, Is.False );
            r.GenerateFinalAssembly( TestHelper.Logger, TestHelper.BinFolder, "TEST_SimpleEmit" );

            IContextRoot c = StObjContextRoot.Load( "TEST_SimpleEmit" );
            Assert.That( typeof(B).IsAssignableFrom( c.Default.MapType( typeof(A) ) ) );
        }
    }
}
