using CK.Core;
using CK.SqlServer;
using CK.Text;
using FluentAssertions;
using NUnit.Framework;
using SqlCallDemo.ComplexType;
using System;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class OutputTypeTests
    {
        [Test]
        public void calling_with_null_sql_default_fails()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<OutputTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                // Here we COULD analyze the mapping and detect this: the sql default is null
                // AND the sql parameter is not "output" => We can conclude that since the mapped
                // property is not nullable, this will fail!
                p.Invoking( _ => _.GetWithSqlDefault( ctx ) ).Should().Throw<InvalidCastException>();
            }
        }

        [Test]
        public void calling_with_null_sql_default_obviously_fails()
        {
            var p = TestHelper.StObjMap.StObjs.Obtain<OutputTypePackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                var r = p.GetWithCSharpDefault( ctx, 3712 );
                r.ParamInt.Should().Be( 3712 );
                r.ParamSmallInt.Should().Be( 37 );
                r.ParamTinyInt.Should().Be( 12 );
                r.Result.Should().Be( "ParamInt: 3712, ParamSmallInt: 37, ParamTinyInt: 12." );
            }
        }
    }
}
