#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests\IntoTheWild\IntoTheWildTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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
        public void IntoTheWildAutoCreated()
        {
            using( var removal = new SqlManager() )
            {
                removal.Open( "." );
                Assert.That( removal.Connection.ExecuteScalar(
                                        @"if db_id('IntoTheWildAutoCreated') is not null drop database IntoTheWildAutoCreated; 
                                          if db_id('IntoTheWildAutoCreated_Histo') is not null drop database IntoTheWildAutoCreated_Histo;
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
            }

            var config = new SqlSetupAspectConfiguration();
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.SetupConfiguration.AppDomainConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );
            config.DefaultDatabaseConnectionString = "Server=.;Database=IntoTheWildAutoCreated;Integrated Security=SSPI";
            config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", "Server=.;Database=IntoTheWildAutoCreated_Histo;Integrated Security=SSPI" ) );

            StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ).Dispose();

            using( var defaultDB = new SqlManager() )
            {
                defaultDB.Open( ".", "IntoTheWildAutoCreated" );
                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
            }
            using( var histoDB = new SqlManager() )
            {
                histoDB.Open( ".", "IntoTheWildAutoCreated_Histo" );
                Assert.That( histoDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
            }
        }

        [Test]
        public void IntoTheWild0()
        {
            var config = new SqlSetupAspectConfiguration();
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.SetupConfiguration.AppDomainConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "IntoTheWild", TestHelper.ConsoleMonitor ) )
            {
                string defaultDBConnectionString = defaultDB.Connection.ConnectionString;
                config.DefaultDatabaseConnectionString = defaultDBConnectionString;
                config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", defaultDBConnectionString ) );

                StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ).Dispose();
                
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

                StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ).Dispose();

                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );

                var map = StObjContextRoot.Load( "IntoTheWild.Auto", StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.ConsoleMonitor );
                Assert.That( map.Default.Obtain<SqlDefaultDatabase>().ConnectionString, Is.EqualTo( defaultDBConnectionString ) );
                Assert.That( map.FindContext("dbHisto").Obtain<SqlHistoDatabase>().ConnectionString, Is.EqualTo( defaultDBConnectionString ) );
            }
        }
    }
}
