using CK.Core;
using CK.SqlServer;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System.Data;
using static CK.Testing.SqlServerTestHelper;

namespace SqlCallDemo.Tests
{
    [TestFixture]
    public class TransactionalTests
    {
        [Test]
        public void setting_an_isolation_level_is_scoped_to_a_stored_procedure()
        {
            var p = SharedEngine.Map.StObjs.Obtain<TransactionalPackage>();
            using( var ctx = new SqlTransactionCallContext( TestHelper.Monitor ) )
            {
                var controller = ctx[p];
                using( var tran = controller.BeginTransaction( IsolationLevel.RepeatableRead ) )
                {
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.RepeatableRead );
                    string previous = p.TransactSetLevelNotWorking( ctx );
                    previous.Should().Be( "REPEATABLE READ" );
                    controller.GetCurrentIsolationLevel().Should().Be( IsolationLevel.RepeatableRead );
                }
            }
        }
    }
}
