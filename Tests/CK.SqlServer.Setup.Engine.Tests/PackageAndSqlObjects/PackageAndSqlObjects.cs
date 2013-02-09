using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Reflection;
using CK.SqlServer;

namespace CK.SqlServer.Setup.Engine.Tests
{

    [TestFixture]
    public class PackageAndSqlObjects
    {
        [Test]
        public void IntoTheWild0()
        {
            string defaultDBConnectionString;
            using( var defaultDB = SqlManager.OpenOrCreate( ".", "IntoTheWild", TestHelper.Logger ) )
            {
                defaultDBConnectionString = defaultDB.Connection.ConnectionString;

                var config = new SqlSetupCenterConfiguration();
                config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", defaultDBConnectionString ) );
                config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
                config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

                // Try normally with any existing database if it exists.
                using( SqlSetupCenter c = new SqlSetupCenter( TestHelper.Logger, config, defaultDB ) )
                {
                    Assert.That( c.Run() );
                }
                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                // Drop CK and CKCore schema.
                {
                    defaultDB.SchemaDropAllObjects( "CK", true );
                    defaultDB.SchemaDropAllObjects( "CKCore", false );
                    Assert.That( defaultDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes','tResDataRawText')" ), Is.EqualTo( 0 ) );
                }
            }
            // Database exists, but is empty. Retries with an autonomous configuration (in reverse order).
            {

                var config = new SqlSetupCenterConfiguration();
                config.DefaultDatabaseConnectionString = defaultDBConnectionString;
                config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", defaultDBConnectionString ) );
                config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
                config.SetupConfiguration.RevertOrderingNames = true;
                config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

                using( SqlSetupCenter c = new SqlSetupCenter( TestHelper.Logger, config ) )
                {
                    Assert.That( c.Run() );
                    Assert.That( c.DefaultSqlDatabase.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                }
            }
        }
    }
}
