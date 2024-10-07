using CK.Core;
using CK.SqlServer;
using CK.Testing;
using NUnit.Framework;
using System;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests;



[TestFixture]
public partial class CallGuidRefTest
{
    [Test]
    public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext()
    {
        var p = SharedEngine.Map.StObjs.Obtain<GuidRefTestPackage>();
        Guid inOut = Guid2;
        string result;
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            p.GuidRefTest( ctx, true, Guid1, ref inOut, out result );
        }
        Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
        Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
    }

    [Test]
    public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext_with_a_return_value()
    {
        var p = SharedEngine.Map.StObjs.Obtain<GuidRefTestPackage>();
        Guid inOut = Guid2;
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            string result = p.GuidRefTestReturn( ctx, true, Guid1, ref inOut );
            Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
            Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }
    }

    [Test]
    public void calling_a_ExecuteNonQuery_method_with_the_standard_SqlStandardCallContext_with_a_return_value_that_is_a_ref_param()
    {
        var p = SharedEngine.Map.StObjs.Obtain<GuidRefTestPackage>();
        Guid inOut = Guid2;
        string result;
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            inOut = p.GuidRefTestReturnInOut( ctx, true, Guid1, inOut, out result );
            Assert.That( inOut, Is.Not.EqualTo( Guid2 ), "Since ReplaceInAndOut was true." );
            Assert.That( result, Is.EqualTo( "@InOnly is not null, @InAndOut is not null." ) );
        }
    }

}
