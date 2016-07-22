using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlTransform.Tests
{
    [TestFixture]
    public class TransformTests
    {
        [Test]
        public void calling_SimpleReplaceTest_method()
        {
            var p = TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string s = p.SimpleReplaceTest( ctx, "Hello!" );
                Assert.That( s, Is.EqualTo( "Return: Hello! 0" ) );
            }
        }

        [Test]
        public void calling_SimpleTransformTest_method()
        {
            var p = TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            var p2 = TestHelper.StObjMap.Default.Obtain<CKLevel2.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string s;
                s = p.SimpleTransormTest( ctx );
                Assert.That( s, Is.EqualTo( "Yes! 0" ) );
                s = p2.SimpleTransformTest( ctx, "unused", 3712 );
                Assert.That( s, Is.EqualTo( "Yes! 3712" ) );
            }
        }

        [Test]
        public void calling_SimplY4TemplateTest_method()
        {
            var p = TestHelper.StObjMap.Default.Obtain<CKLevel0.Package>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string s = p.SimplY4TemplateTest( ctx );
                Assert.That( s, Is.StringMatching( @"HashCode = \d+" ) );
            }
        }
    }
}
