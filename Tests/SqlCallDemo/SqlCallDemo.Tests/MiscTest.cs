using CK.SqlServer;
using CK.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

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
            p.Invoking( t => t.CanWaitForTheDefaultCommandTimeout( ctx, 2 ) ).Should().NotThrow();
            p.Invoking( t => t.CanWaitOnlyForOneSecond( ctx, 2 ) ).Should().Throw<SqlDetailedException>();

            SqlHelper.CommandTimeoutFactor = 3;
            p.Invoking( t => t.CanWaitOnlyForOneSecond( ctx, 2 ) ).Should().NotThrow();
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
            p.VerbatimParameterAtWork( ctx, @this, @operator ).Should().Be( 3712 );
        }
    }



}
