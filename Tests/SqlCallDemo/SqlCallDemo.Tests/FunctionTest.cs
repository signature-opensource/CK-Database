using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.SqlServer.Setup;
using NUnit.Framework;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class FunctionTest
    {
        [Test]
        public async Task async_call_returns_string()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string t1 = await p.StringFunctionAsync( ctx, 3712 );
                string t2 = await p.StringFunctionAsync( ctx, 2173 );
                Assert.That( t1, Is.EqualTo( "@V = 3712" ) );
                Assert.That( t2, Is.EqualTo( "@V = 2173" ) );
            }
        }

        [Test]
        public void call_returns_string()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string t1 = p.StringFunction( ctx, 3712 );
                string t2 = p.StringFunction( ctx, 2173 );
                Assert.That( t1, Is.EqualTo( "@V = 3712" ) );
                Assert.That( t2, Is.EqualTo( "@V = 2173" ) );
            }
        }

        [Test]
        public async Task async_call_returns_byte()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                byte t1 = await p.ByteFunctionAsync( ctx, 2 );
                byte t2 = await p.ByteFunctionAsync( ctx, 4 );
                Assert.That( t1, Is.EqualTo( 4 ) );
                Assert.That( t2, Is.EqualTo( 16 ) );
            }
        }

        [Test]
        public void call_returns_byte()
        {
            var p = TestHelper.StObjMap.Default.Obtain<FunctionPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                byte t1 = p.ByteFunction( ctx, 3 );
                byte t2 = p.ByteFunction( ctx, 5 );
                Assert.That( t1, Is.EqualTo( 9 ) );
                Assert.That( t2, Is.EqualTo( 25 ) );
            }
        }

    }
}
