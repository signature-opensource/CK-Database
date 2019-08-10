using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System.Data.SqlClient;
using static CK.Testing.DBSetupTestHelper;

namespace CK.SqlServer.Setup.Engine.Tests.Core
{
    [TestFixture]
    public class SqlManagerTests
    {
        [Test]
        public void SqlManager_OpenFromConnectionString_catch_any_error()
        {
            using( SqlManager m = new SqlManager( TestHelper.Monitor ) )
            {
                Assert.That( m.OpenFromConnectionString( "invalid connection string", true ), Is.False );
            }
        }

        [Test]
        public void DBSetup_can_not_touch_master_model_tempdb_or_msdb()
        {
            var badTarget = TestHelper.GetDatabaseOptions( "master" );
            TestHelper.RunDBSetup( badTarget ).Should().Be( CKSetup.CKSetupRunResult.Failed );

            using( var db = new SqlConnection( TestHelper.MasterConnectionString ) )
            {
                db.Open();
                using( var cmd = new SqlCommand( "select DB_Name()", db ) )
                {
                    cmd.ExecuteScalar().Should().Be( "master" );
                    cmd.CommandText = "select count(*) from sys.tables where name = 'tSystem';";
                    cmd.ExecuteScalar().Should().Be( 0 );
                }
            }

            badTarget = TestHelper.GetDatabaseOptions( "msdb" );
            TestHelper.RunDBSetup( badTarget ).Should().Be( CKSetup.CKSetupRunResult.Failed );

            badTarget = TestHelper.GetDatabaseOptions( "model" );
            TestHelper.RunDBSetup( badTarget ).Should().Be( CKSetup.CKSetupRunResult.Failed );

            badTarget = TestHelper.GetDatabaseOptions( "tempdb" );
            TestHelper.RunDBSetup( badTarget ).Should().Be( CKSetup.CKSetupRunResult.Failed );
        }

    }
}
