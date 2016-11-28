using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using NUnit.Framework;
using SqlCallDemo.ComplexType;

namespace SqlCallDemo.Tests
{
    /*
     * Not implemented. See ImplicitConversionPackage.
     * 
     * 
    [TestFixture]
    public class ImplicitConversionTests
    {
        [Test]
        public void using_implicit_conversion_on_string()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ImplicitConversionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var r = p.CallSyncScopes( ctx, 3, new StringWrapper( "openid" ) );
                Assert.That( r, Is.EqualTo( "3 - openid" ) );
            }
        }
    }
    */
}
