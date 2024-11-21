using FluentAssertions;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using static CK.Testing.SqlServerTestHelper;
using System.Configuration;
using CK.Testing;
using CK.Setup;
using System.Threading.Tasks;

namespace CK.SqlServer.Setup.Engine.Tests.Core;

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
    public async Task DBSetup_can_not_touch_master_model_tempdb_or_msdb_Async()
    {
        var badTarget = TestHelper.GetDatabaseOptions( "master" );
        var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
        engineConfiguration.EnsureSqlServerConfigurationAspect( badTarget );
        (await engineConfiguration.RunAsync()).Status.Should().Be( RunStatus.Failed );

        using( var db = new SqlConnection( TestHelper.MasterConnectionString ) )
        {
            await db.OpenAsync();
            using( var cmd = new SqlCommand( "select DB_Name()", db ) )
            {
                (await cmd.ExecuteScalarAsync()).Should().Be( "master" );
                cmd.CommandText = "select count(*) from sys.tables where name = 'tSystem';";
                (await cmd.ExecuteScalarAsync()).Should().Be( 0 );
            }
        }

        var sqlAspectConfiguration = engineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>();

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "msdb" );
        (await engineConfiguration.RunAsync()).Status.Should().Be( RunStatus.Failed );

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "model" );
        (await engineConfiguration.RunAsync()).Status.Should().Be( RunStatus.Failed );

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "tempdb" );
        (await engineConfiguration.RunAsync()).Status.Should().Be( RunStatus.Failed );
    }

}
