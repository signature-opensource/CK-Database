using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using NUnit.Framework;

namespace CK.SqlServer.Setup.Engine.Tests.Core
{
    [TestFixture]
    public class SqlManagerTests
    {
        [Test]
        public void SqlManager_OpenOrCreate_catch_any_errors_when_a_Monitor_is_set()
        {
            using( SqlManager m = new SqlManager() { Monitor = TestHelper.ConsoleMonitor } )
            {
                Assert.That( m.OpenOrCreate( ".", "Invalid-Database-Name" ), Is.False );
            }
        }

        [Test]
        public void an_invalid_database_name_does_not_DBSetup_master()
        {
            var config = new SqlSetupAspectConfiguration();
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.SetupConfiguration.AppDomainConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );
            config.DefaultDatabaseConnectionString = "Server=.;Database=INVALID-NAME-TEST;Integrated Security=SSPI";

            StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ).Dispose();

            using( var defaultDB = new SqlManager() )
            {
                defaultDB.Open( "." );
                Assert.That( defaultDB.Connection.ExecuteScalar( "select DB_Name()" ), Is.EqualTo( "master" ) );
                Assert.That( defaultDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name = 'tSystem';" ), Is.EqualTo( 0 ) );
            }
        }
    }
}
