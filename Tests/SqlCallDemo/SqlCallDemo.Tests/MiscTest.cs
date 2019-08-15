using CK.Core;
using CK.SqlServer;
using FluentAssertions;
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
            var p = TestHelper.StObjMap.StObjs.Obtain<MiscPackage>();
            using( var ctx = new SqlStandardCallContext() )
            {
                p.Invoking( t => t.CanWaitForTheDefaultCommandTimeout( ctx, 2 ) ).Should().NotThrow();
                p.Invoking( t => t.CanWaitOnlyForOneSecond( ctx, 2 ) ).Should().Throw<SqlDetailedException>();

                SqlHelper.CommandTimeoutFactor = 3;
                p.Invoking( t => t.CanWaitOnlyForOneSecond( ctx, 2 ) ).Should().NotThrow();
            }
        }

    }
}
