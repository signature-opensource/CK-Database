using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using SqlCallDemo.ComplexType;
using System;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class OutputTypeTests
    {
        [Test]
        public void calling_with_sql_default()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<OutputTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var r = p.GetWithSqlDefault( ctx );
                r.ParamInt.Should().Be( 0 );
                r.ParamSmallInt.Should().Be( 0 );
                r.ParamTinyInt.Should().Be( 0 );
                r.Result.Should().Be( "ParamInt: , ParamSmallInt: , ParamTinyInt: ." );
            }
        }
    }
}
