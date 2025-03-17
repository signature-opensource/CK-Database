using CK.SqlServer;
using CK.Testing;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;
using CK.Core;

namespace SqlCallDemo.Tests;

[TestFixture]
public class MiscTest
{
    [Test]
    public void timeout_configuration_is_available_on_callables()
    {
        var p = SharedEngine.AutomaticServices.GetRequiredService<MiscPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            Util.Invokable( () => p.CanWaitForTheDefaultCommandTimeout( ctx, 2 ) ).ShouldNotThrow();
            Util.Invokable( () => p.CanWaitOnlyForOneSecond( ctx, 2 ) ).ShouldThrow<SqlDetailedException>();

            SqlHelper.CommandTimeoutFactor = 3;
            Util.Invokable(() => p.CanWaitOnlyForOneSecond(ctx, 2)).ShouldNotThrow();
        }
    }

    [Test]
    public void verbatim_parameter_names()
    {
        var p = SharedEngine.AutomaticServices.GetRequiredService<MiscPackage>();
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            int @this = 3700;
            int @operator = 12;
            p.VerbatimParameterAtWork( ctx, @this, @operator ).ShouldBe( 3712 );
        }
    }



}
