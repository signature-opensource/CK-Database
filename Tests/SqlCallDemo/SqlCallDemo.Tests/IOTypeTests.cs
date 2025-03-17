using CK.Core;
using CK.SqlServer;
using CK.Testing;
using Shouldly;
using NUnit.Framework;
using SqlCallDemo.ComplexType;
using System;
using System.Text;
using System.Threading.Tasks;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests;

[TestFixture]
public class IOTypeTests
{
    [Test]
    public void calling_with_null_sql_default_fails()
    {
        var p = SharedEngine.Map.StObjs.Obtain<IOTypePackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            // Here we COULD analyze the mapping and detect this: the sql default is null
            // AND the sql parameter is not "output" => We can conclude that since the mapped
            // property is not nullable, this will fail!
            // But for the moment, we don't.
            Util.Invokable( () => p.GetWithSqlDefault( ctx ) ).ShouldThrow<InvalidCastException>();
        }
    }

    [Test]
    public void calling_with_csharp_defaults()
    {
        var p = SharedEngine.Map.StObjs.Obtain<IOTypePackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var r = p.GetWithCSharpDefault( ctx, 3712 );
            r.ParamInt.ShouldBe( 3712 );
            r.ParamSmallInt.ShouldBe( 37 );
            r.ParamTinyInt.ShouldBe( 12 );
            r.Result.ShouldBe( "ParamInt: 3712, ParamSmallInt: 37, ParamTinyInt: 12." );
        }
    }

    [Test]
    public void calling_with_parameter_source()
    {
        var p = SharedEngine.Map.StObjs.Obtain<IOTypePackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var r = p.GetWithInputType( ctx, new InputTypeCastWithDefault { ParamInt = 3712, ParamSmallInt = 37, ParamTinyInt = 12 } );
            r.ShouldBe( "ParamInt: 3712, ParamSmallInt: 37, ParamTinyInt: 12." );
        }
    }
}
