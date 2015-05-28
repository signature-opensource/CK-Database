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
    public class ReturnTest
    {
        [Test]
        public async Task async_call_returns_string()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ReturnPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                string t1 = await p.StringReturnAsync( ctx, 3712 );
                string t2 = await p.StringReturnAsync( ctx, 2173 );
                Assert.That( t1, Is.EqualTo( "@V = 3712" ) );
                Assert.That( t2, Is.EqualTo( "@V = 2173" ) );
            }
        }

        [Test]
        public async Task async_call_returns_int()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ReturnPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                int iNull = await p.IntReturnAsync( ctx, null );
                int i = await p.IntReturnAsync( ctx, 3712 );
                Assert.That( iNull, Is.EqualTo( -1 ) );
                Assert.That( i, Is.EqualTo( 3712*3712 ) );
            }
        }

        [Test]
        public async Task async_call_returns_int_with_actor_context()
        {
            var p = TestHelper.StObjMap.Default.Obtain<ReturnPackage>();
            using( var ctx = new TestActorContext( 3712 ) )
            {
                int i = await p.IntReturnWithActorAsync( ctx );
                Assert.That( i, Is.EqualTo( 3712 * 3712 * 5 ) );
                int j = await p.IntReturnWithActorAsync( ctx, "12" );
                Assert.That( j, Is.EqualTo( 3712 * 3712 * 12 ) );
            }
        }

    }
}
