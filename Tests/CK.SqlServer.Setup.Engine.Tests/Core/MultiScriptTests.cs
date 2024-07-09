using NUnit.Framework;
using static CK.Testing.SqlServerTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class MultiScriptTests
    {
        [Test]
        public void ErrorInDirectScripts()
        {
            using( SqlManager m = ErrorHandlingTests.CreateInstallContext() )
            {
                TestScriptOnError( m, @"
-- Statement aborting error.
truncate table CKCoreTests.tTestErrorLogTestResult;
--[beginscript]
insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('Should be cleared!');
exec('syntax-errror');
--[endscript]", null );

                TestScriptOnError( m, @"
-- Batch aborting error.
truncate table CKCoreTests.tTestErrorLogTestResult;
--[beginscript]
insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('Should be cleared!');
declare @BatchAborts datetime = convert(datetime, '2008111');
--[endscript]", null );
            }
        }

        [Test]
        public void ErrorInTransactedScripts()
        {
            using( SqlManager m = ErrorHandlingTests.CreateInstallContext() )
            {

                TestScriptOnError( m, @"
-- Statement aborting error in an outer transaction.
begin tran
truncate table CKCoreTests.tTestErrorLogTestResult;
--[beginscript]
insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('Should be cleared!');
exec('syntax-errror');
--[endscript]
if exists( select * from CKCoreTests.tTestErrorLogTestResult )
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('BUG: The inner script should have been locally rollbacked!');
end
else
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('OK');
end
commit;
", "OK" );

                TestScriptOnError( m, @"
-- Batch aborting error in an outer transaction.

begin tran
truncate table CKCoreTests.tTestErrorLogTestResult;

--[beginscript]

insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('Should be cleared!');

declare @BatchAborts datetime = convert(datetime, '2008111');

--[endscript]

if @@TRANCOUNT <> 0
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('BUG: The rollback has been done');
end
else
if XACT_STATE() <> 0
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('BUG: The XACT_STATE should be 0 (we are no more in the doomed transaction).');
end
else
if exists( select * from CKCoreTests.tTestErrorLogTestResult )
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('BUG: The inner script should have been locally rollbacked!');
end
else
begin
    insert into CKCoreTests.tTestErrorLogTestResult(Error) values ('OK');
end
", "OK" );

            }
        }

        private static void TestScriptOnError( SqlManager m, string script, string expected )
        {
            var sC = new SimpleScriptTagHandler( script );
            Assert.That( sC.Expand( TestHelper.Monitor, true ) );
            var s = sC.SplitScript();
            Assert.That( s.Count, Is.AtLeast( 2 ) );

            using( var e = m.CreateExecutor( TestHelper.Monitor ) )
            {
                Assert.That( e.Execute( s[0].Body ) );
                Assert.That( e.Execute( s[1].Body ), Is.False );
                if( s.Count > 2 ) Assert.That( e.Execute( s[2].Body ) );
                if( expected == null )
                {
                    Assert.That( m.ExecuteScalar( "select Error from CKCoreTests.tTestErrorLogTestResult;" ), Is.Null );
                }
                else
                {
                    Assert.That( m.ExecuteScalar( "select Error from CKCoreTests.tTestErrorLogTestResult;" ), Is.EqualTo( expected ) );
                }
            }
        }


    }
}
