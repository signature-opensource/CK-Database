using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;
using CK.SqlServer;
using CK.Core;

namespace CK.SqlServer.Setup.Engine.Tests
{

    [TestFixture]
    public class IntoTheWildTests
    {
        [Test]
        public void IntoTheWild0()
        {
            var config = new SqlSetupCenterConfiguration();
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.SetupConfiguration.AppDomainConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "IntoTheWild", TestHelper.Logger ) )
            {
                string defaultDBConnectionString = defaultDB.Connection.ConnectionString;
                config.DefaultDatabaseConnectionString = defaultDBConnectionString;
                config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", defaultDBConnectionString ) );

                StObjContextRoot.Build( config, TestHelper.Logger ).Dispose();
                
                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                // Drop CK and CKCore schema.
                {
                    defaultDB.SchemaDropAllObjects( "CK", true );
                    defaultDB.SchemaDropAllObjects( "CKCore", false );
                    Assert.That( defaultDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes','tResDataRawText')" ), Is.EqualTo( 0 ) );
                }
                
                // Database exists, but is empty. Retries with an autonomous configuration (in reverse order) and
                // generates the assemly to test ConnectionString injection on SqlDatabase objects.
                config.SetupConfiguration.RevertOrderingNames = true;
                config.SetupConfiguration.FinalAssemblyConfiguration.AssemblyName = "IntoTheWild.Auto";
                config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = false;
                config.SetupConfiguration.AppDomainConfiguration.UseIndependentAppDomain = true;

                StObjContextRoot.Build( config, TestHelper.Logger ).Dispose();

                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );

                var map = StObjContextRoot.Load( "IntoTheWild.Auto", StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.Logger );
                Assert.That( map.Default.Obtain<SqlDefaultDatabase>().ConnectionString, Is.EqualTo( defaultDBConnectionString ) );
                Assert.That( map.FindContext("dbHisto").Obtain<SqlHistoDatabase>().ConnectionString, Is.EqualTo( defaultDBConnectionString ) );
            }
        }
    }
}
