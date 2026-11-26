using CK.Core;
using Shouldly;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using static CK.Testing.MonitorTestHelper;
using System.Configuration;
using CK.Testing;
using CK.Setup;
using System.Linq;
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
        (await engineConfiguration.RunAsync()).Status.ShouldBe( RunStatus.Failed );

        using( var db = new SqlConnection( TestHelper.MasterConnectionString ) )
        {
            await db.OpenAsync();
            using( var cmd = new SqlCommand( "select DB_Name()", db ) )
            {
                (await cmd.ExecuteScalarAsync()).ShouldBe( "master" );
                cmd.CommandText = "select count(*) from sys.tables where name = 'tSystem';";
                (await cmd.ExecuteScalarAsync()).ShouldBe( 0 );
            }
        }

        var sqlAspectConfiguration = engineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>();

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "msdb" );
        (await engineConfiguration.RunAsync()).Status.ShouldBe( RunStatus.Failed );

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "model" );
        (await engineConfiguration.RunAsync()).Status.ShouldBe( RunStatus.Failed );

        sqlAspectConfiguration.DefaultDatabaseConnectionString = TestHelper.GetConnectionString( "tempdb" );
        (await engineConfiguration.RunAsync()).Status.ShouldBe( RunStatus.Failed );
    }

    [Test]
    public async Task a_setup_does_not_log_the_password_of_its_connection_string_Async()
    {
        var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
        engineConfiguration.EnsureSqlServerConfigurationAspect();
        // The connection string of the tests, with a password. With the Windows account, the server ignores
        // a login and a password: a marker is added.
        var b = new SqlConnectionStringBuilder( TestHelper.GetConnectionString() );
        if( b.IntegratedSecurity )
        {
            b.UserID = "IgnoredLogin";
            b.Password = "Ignored-S3cret-Marker";
        }
        var password = b.Password;
        engineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>().DefaultDatabaseConnectionString = b.ConnectionString;

        using( TestHelper.Monitor.TemporarilySetMinimalFilter( LogLevelFilter.Debug, LogLevelFilter.Debug ) )
        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Debug, 100_000 ) )
        {
            await engineConfiguration.RunAsync();
            // Counts are compared: a failed assertion must not display the entries.
            var connections = entries.Where( e => e.Text.StartsWith( "Connection to " ) ).ToArray();
            connections.Length.ShouldBeGreaterThan( 0 );
            connections.Count( e => e.Text.Contains( password ) ).ShouldBe( 0 );
            var configured = entries.Where( e => e.Text.StartsWith( "SqlDefaultDatabase.ConnectionString configured to" ) ).ToArray();
            configured.Length.ShouldBeGreaterThan( 0 );
            configured.Count( e => e.Text.Contains( password ) ).ShouldBe( 0 );
        }
    }

    [Test]
    public async Task a_failed_connection_does_not_log_the_password_of_its_connection_string_Async()
    {
        var engineConfiguration = TestHelper.CreateDefaultEngineConfiguration();
        engineConfiguration.EnsureSqlServerConfigurationAspect();
        // A login that does not exist: the server refuses the connection.
        const string password = "S3cret-Marker-Of-A-Login-That-Does-Not-Exist";
        var b = new SqlConnectionStringBuilder( TestHelper.GetConnectionString() )
        {
            IntegratedSecurity = false,
            UserID = "CKTEST_NoSuchLogin",
            Password = password
        };
        engineConfiguration.EnsureAspect<SqlSetupAspectConfiguration>().DefaultDatabaseConnectionString = b.ConnectionString;

        using( TestHelper.Monitor.CollectEntries( out var entries, LogLevelFilter.Debug, 100_000 ) )
        {
            (await engineConfiguration.RunAsync()).Status.ShouldBe( RunStatus.Failed );
            var unable = entries.Where( e => e.Text.StartsWith( "Unable to create database for" ) ).ToArray();
            unable.Length.ShouldBeGreaterThan( 0 );
            unable.Count( e => e.Text.Contains( password ) ).ShouldBe( 0 );
            var connections = entries.Where( e => e.Text.StartsWith( "Connection to " ) ).ToArray();
            connections.Length.ShouldBeGreaterThan( 0 );
            connections.Count( e => e.Text.Contains( password ) ).ShouldBe( 0 );
        }
    }

}
