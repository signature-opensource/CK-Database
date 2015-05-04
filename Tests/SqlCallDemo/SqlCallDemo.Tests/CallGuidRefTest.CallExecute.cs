using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.SqlServer;
using CK.SqlServer.Setup;
using NUnit.Framework;

namespace SqlCallDemo.Tests
{


    [TestFixture]
    public partial class CallGuidRefTest
    {
        [Test]
        public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            string result;
            using( var ctx = new SqlStandardCallContext() )
            {
                p.GuidRefTest( ctx, true, Guid1, ref inOut, out result );
            }
            Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
            Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }

        [Test]
        public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext_with_a_return_value()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            using( var ctx = new SqlStandardCallContext() )
            {
                string result = p.GuidRefTestReturn( ctx, true, Guid1, ref inOut );
                Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
                Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
            }
        }

        [Test]
        public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext_with_a_return_value_that_is_a_ref_param()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            string result;
            using( var ctx = new SqlStandardCallContext() )
            {
                inOut = p.GuidRefTestReturnInOut( ctx, true, Guid1, inOut, out result );
                Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
                Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
            }
        }


        public class NonStandardSqlCallContext : SqlStandardCallContext, INonStandardSqlCallContextSpecialized
        {
        }   
    
        [Test]
        public void calling_a_ExecuteNonQuery_method_with_a_non_standard_SqlCallContext_with_a_return_value()
        {
            var p = TestHelper.StObjMap.Default.Obtain<GuidRefTestPackage>();
            Guid inOut = Guid2;
            using( var ctx = new NonStandardSqlCallContext() )
            {
                string result = p.GuidRefTestReturnWithInterfaceContext( ctx, true, Guid1, ref inOut );
                Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
                Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
            }
        }


    }
}
