using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;
using System.Reflection.Emit;

namespace CK.Setup.StObj.Tests
{

    [TestFixture]
    public class AutoImplementationTests
    {

        [AttributeUsage( AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
        public class AutoImplementMethodAttribute : Attribute, IAutoImplementorMethod
        {
            public bool Implement( IActivityLogger logger, MethodInfo m, TypeBuilder b )
            {
                CK.Reflection.EmitHelper.ImplementStubMethod( b, m );
                return true;
            }
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
                Assert.That( result.Default.StObjMapper.GetObject<A>(), Is.Not.Null );
            }

        }

    }
}
