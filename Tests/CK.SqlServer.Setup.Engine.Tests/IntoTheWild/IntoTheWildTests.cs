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
using CK.Setup;
using System.Data.SqlClient;

namespace CK.SqlServer.Setup.Engine.Tests
{

    [TestFixture]
    public class IntoTheWildTests
    {

        const string dbDefault = "Server=.;Database=IntoTheWild0;Integrated Security=SSPI";
        const string dbHisto = "Server=.;Database=IntoTheWild0_Histo;Integrated Security=SSPI";

        [Test]
        public void IntoTheWildAutoCreated()
        {
            Assume.That( false, "Objects dispatching across contexts and links to their Database (the location) has to be refactored." );
            DropDatabases();

            var c = new SetupEngineConfiguration();
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

            var config = new SqlSetupAspectConfiguration();
            config.DefaultDatabaseConnectionString = "Server=.;Database=IntoTheWildAutoCreated;Integrated Security=SSPI";
            config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", "Server=.;Database=IntoTheWildAutoCreated_Histo;Integrated Security=SSPI" ) );
            c.Aspects.Add( config );

            using( var r = StObjContextRoot.Build( c, null, TestHelper.ConsoleMonitor ) )
            {
                Assert.That( r.Success );
            }
            DropDatabases();
        }

        static void DropDatabases()
        {
            using( var removal = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                using( var cClean = new SqlConnection( dbDefault ) )
                {
                    SqlConnection.ClearPool( cClean );
                }
                removal.OpenFromConnectionString( TestHelper.MasterConnection );
                Assert.That( removal.Connection.ExecuteScalar(
                                        @"if db_id('IntoTheWildAutoCreated_Histo') is not null drop database IntoTheWildAutoCreated_Histo;
                                                          select 'Done';" ), Is.EqualTo( "Done" ) );
                using( var cClean = new SqlConnection( dbHisto ) )
                {
                    SqlConnection.ClearPool( cClean );
                }
                Assert.That( removal.Connection.ExecuteScalar(
                                        @"if db_id('IntoTheWildAutoCreated') is not null drop database IntoTheWildAutoCreated; 
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
            }
        }

        [Test]
        public void IntoTheWild0()
        {
            Assume.That( false, "Objects dispatching across contexts and links to their Database (the location) has to be refactored." );

            using( var defaultDB = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                defaultDB.OpenFromConnectionString( dbDefault, true );
                defaultDB.SchemaDropAllObjects( "CK", true );
                defaultDB.SchemaDropAllObjects( "CKCore", false );
            } 
            using( var histoDB = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                histoDB.OpenFromConnectionString( dbHisto, true );
                histoDB.SchemaDropAllObjects( "CK", true );
                histoDB.SchemaDropAllObjects( "CKCore", false );
            }

            var c = new SetupEngineConfiguration();
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.UseIndependentAppDomain = true;
            var config = new SqlSetupAspectConfiguration();
            config.DefaultDatabaseConnectionString = dbDefault;
            config.Databases.Add( new SqlDatabaseDescriptor( "dbHisto", dbHisto ) );
            c.Aspects.Add( config );

            using( var r = StObjContextRoot.Build( c, null, TestHelper.ConsoleMonitor ) )
            {
                Assert.That( r.Success );
            }
            
            using( var defaultDB = new SqlManager( TestHelper.ConsoleMonitor ) )
            using( var histoDB = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                defaultDB.OpenFromConnectionString( dbDefault );
                histoDB.OpenFromConnectionString( dbHisto );

                Assert.That( histoDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                // Drop CK and CKCore schema from histoDB.
                {
                    histoDB.SchemaDropAllObjects( "CK", true );
                    histoDB.SchemaDropAllObjects( "CKCore", false );
                    Assert.That( histoDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes')" ), Is.EqualTo( 0 ) );
                    // Database histo exists, but is empty. Version and Setup information are in defaultDB.
                    // Updates Version information to state that there is no more objects in dbHisto (location part of the full name).
                    defaultDB.Connection.ExecuteNonQuery( "delete from CKCore.tItemVersion where FullName like '[[]%]dbHisto^%'" );
                }
                Assert.That( defaultDB.Connection.ExecuteScalar( "select ResName from CK.tRes where ResId=1" ), Is.EqualTo( "System" ) );
                
                // Retries with the configuration (in reverse order) and
                // generates the assemly to test ConnectionString injection on SqlDatabase objects.
                c.RevertOrderingNames = true;
                c.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = "IntoTheWild.Auto";
                c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = false;

                using( var r = StObjContextRoot.Build( c, null, TestHelper.ConsoleMonitor ) )
                {
                    Assert.That( r.Success );
                }

                Assert.That( histoDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tRes')" ), Is.EqualTo( 2 ) );

                var map = StObjContextRoot.Load( "IntoTheWild.Auto", StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.ConsoleMonitor );
                Assert.That( map.Default.Obtain<SqlDefaultDatabase>().ConnectionString, Is.EqualTo( dbDefault ) );
                Assert.That( map.FindContext( "dbHisto" ).Obtain<SqlHistoDatabase>().ConnectionString, Is.EqualTo( dbHisto ) );

                Assert.That( map.FindContext( "dbHisto" ).Obtain<IntoTheWild0.ResHome>().Database.ConnectionString, Is.EqualTo( dbHisto ) );
                Assert.That( map.Default.Obtain<IntoTheWild0.ResHome>().Database.ConnectionString, Is.EqualTo( dbDefault ) );
            }
        }
    }
}
