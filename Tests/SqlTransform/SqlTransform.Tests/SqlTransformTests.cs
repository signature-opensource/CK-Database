using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transform.Tests
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
    }
}
