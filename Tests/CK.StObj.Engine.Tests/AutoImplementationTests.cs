using System;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{

    [TestFixture]
    public class AutoImplementationTests
    {

        [AttributeUsage( AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
        public class AutoImplementMethodAttribute : Attribute, IAttributeAutoImplemented
        {
        }

        internal abstract class ABase
        {
            [AutoImplementMethod]
            protected abstract int FirstMethod( int i );
        }

        internal abstract class A : ABase, IAmbientContract
        {
            [AutoImplementMethod]
            public abstract string SecondMethod( int i );
        }

        internal abstract class A2 : A
        {
            [AutoImplementMethod]
            public abstract A ThirdMethod( int i, string s );
        }

        [Test]
        public void AbstractDetection()
        {
            {
                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterClass( typeof( A2 ) );
                StObjCollectorResult result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.False );
                Assert.That( result.Default.StObjMap.Obtain<A>(), Is.Not.Null );
            }

        }

    }
}
