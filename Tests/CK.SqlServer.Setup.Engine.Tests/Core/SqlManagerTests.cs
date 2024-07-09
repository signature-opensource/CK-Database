using FluentAssertions;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using static CK.Testing.SqlServerTestHelper;
using System.Configuration;
using CK.Testing;
using CK.Setup;

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
            var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
            engineConfiguration.EnsureSqlServerConfigurationAspect( badTarget );
            engineConfiguration.Run().Status.Should().Be( RunStatus.Failed );

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

            var sqlAspectConfiguration = engineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>();

            sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "msdb" );
            engineConfiguration.Run().Status.Should().Be( RunStatus.Failed );

            sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "model" );
            engineConfiguration.Run().Status.Should().Be( RunStatus.Failed );

            sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "tempdb" );
            engineConfiguration.Run().Status.Should().Be( RunStatus.Failed );
        }

    }
}
