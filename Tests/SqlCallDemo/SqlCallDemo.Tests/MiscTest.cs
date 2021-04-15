using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static CK.Testing.DBSetupTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class MiscTest
    {
        [Test]
        public void timeout_configuration_is_available_on_callables()
        {
            var p = TestHelper.AutomaticServices.GetRequiredService<MiscPackage>();
            using( var ctx = new SqlStandardCallContext() )
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
            var p = TestHelper.AutomaticServices.GetRequiredService<MiscPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                int @this = 3700;
                int @operator = 12;
                p.VerbatimParameterAtWork( ctx, @this, @operator ).Should().Be( 3712 ); 
            }
        }



    }
}
