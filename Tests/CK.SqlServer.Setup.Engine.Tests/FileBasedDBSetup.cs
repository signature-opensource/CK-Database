#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests\FileBasedDBSetup.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;
using CK.Setup;
using System.IO;
using System.Data.SqlClient;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    [Category("DBSetup")]
    public class FileBasedDBSetup
    {
        static readonly string dbFromScratch = TestHelper.GetConnectionString( "TEST_FROM_SCRATCH" );
        static readonly string dbFromScratchWithView = TestHelper.GetConnectionString( "TEST_FROM_SCRATCH_VIEW" );

        [Test]
        public void InstallFromScratch()
        {
            SqlSetupAspectConfiguration config = new SqlSetupAspectConfiguration();
            config.FilePackageDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratch" ) );
            config.SqlFileDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratch" ) );
            config.DefaultDatabaseConnectionString = dbFromScratch;

            var c = new SetupEngineConfiguration();
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.GenerateFinalAssemblyOption = BuilderFinalAssemblyConfiguration.GenerateOption.DoNotGenerateFile;
            c.Aspects.Add( config );

            using( var defaultDB = SqlManager.OpenOrCreate( dbFromScratch, TestHelper.Monitor ) )
            {
                defaultDB.SchemaDropAllObjects( "Test", true );
                defaultDB.SchemaDropAllObjects( "CKCore", false );

                using( TestHelper.Monitor.OpenTrace().Send( "First setup (will fail due to a MissingDependencyIsError configuration)." ) )
                {
                    using( var r = StObjContextRoot.Build( c, null, TestHelper.Monitor ) )
                    {
                        Assert.That( r.Success, Is.False );
                    }
                }
                config.IgnoreMissingDependencyIsError = true;
                using( TestHelper.Monitor.OpenTrace().Send( "Second setup (will succeed since SqlSetupCenterConfiguration.IgnoreMissingDependencyIsError is true)." ) )
                {
                    using( var r = StObjContextRoot.Build( c, null, TestHelper.Monitor ) )
                    {
                        Assert.That( r.Success );
                    }
                }

                defaultDB.ExecuteOneScript( "drop procedure Test.sOneStoredProcedure;" );
                defaultDB.ExecuteOneScript( "drop function Test.fTest;" );

                using( TestHelper.Monitor.OpenTrace().Send( "Third setup." ) )
                {
                    using( var r = StObjContextRoot.Build( c, null, TestHelper.Monitor ) )
                    {
                        Assert.That( r.Success );
                    }
                }
            }
        }

        [Test]
        public void InstallPackageWithView()
        {
            SqlSetupAspectConfiguration config = new SqlSetupAspectConfiguration();
            config.FilePackageDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratchWithView" ) );
            config.SqlFileDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratchWithView" ) );
            config.IgnoreMissingDependencyIsError = true;
            config.DefaultDatabaseConnectionString = dbFromScratchWithView;
            SetupEngineConfiguration c = new SetupEngineConfiguration();
            c.Aspects.Add( config );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.GenerateFinalAssemblyOption = BuilderFinalAssemblyConfiguration.GenerateOption.DoNotGenerateFile;

            using( var r = StObjContextRoot.Build( c, null, TestHelper.Monitor ) )
            {
                Assert.That( r.Success );
            }

            using( var defaultDB = SqlManager.OpenOrCreate( dbFromScratchWithView, TestHelper.Monitor ) )
            {
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
                defaultDB.Connection.ExecuteNonQuery( "drop view dbo.vTestView" );
                defaultDB.Connection.ExecuteNonQuery( "drop table dbo.tTestV" );
                defaultDB.SchemaDropAllObjects( "CKCore", false );
            }
            // From scratch now: the database is empty.

            using( var r = StObjContextRoot.Build( c, null, TestHelper.Monitor ) )
            {
                Assert.That( r.Success );
            }

            using( var defaultDB = SqlManager.OpenOrCreate( dbFromScratchWithView, TestHelper.Monitor ) )
            {
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
            }
        }

        [Test]
        public void InstallPackageWithSPDependsOnVersion()
        {
            TestHelper.LogToConsole = true;
            try
            {

                using( var defaultDB = SqlManager.OpenOrCreate( dbFromScratch, TestHelper.Monitor ) )
                {
                    defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tSetupMemoryItem]') is not null delete from [CKCore].[tSetupMemoryItem] where ItemKey like '%WithSPDependsOnVersion%';" );
                    defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tItemVersion]') is not null delete from [CKCore].[tItemVersion] where FullName like '%WithSPDependsOnVersion%';" );

                    defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[tTestVSP]') is not null drop table dbo.tTestVSP;" ); // Reset
                    defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[sStoredProcedureWithSPDependsOnVersion]') is not null drop procedure [dbo].[sStoredProcedureWithSPDependsOnVersion];" );

                    SqlSetupAspectConfiguration config = new SqlSetupAspectConfiguration();
                    config.FilePackageDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratchWithSPDependsOnVersion" ) );
                    config.SqlFileDirectories.Add( Path.Combine( TestHelper.ProjectFolder, "Scripts/InstallFromScratchWithSPDependsOnVersion" ) );

                    config.DefaultDatabaseConnectionString = defaultDB.Connection.InternalConnection.ConnectionString;
                    SetupEngineConfiguration c = new SetupEngineConfiguration();
                    c.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = "InstallPackageWithSPDependsOnVersion";
                    c.Aspects.Add( config );

                    var engine = new SetupEngine( TestHelper.Monitor, c, StObjContextRoot.DefaultStObjRuntimeBuilder );

                    StObjContextRoot.Build( c, null, TestHelper.Monitor ).Dispose();
                }
                using( var db = new SqlConnectionProvider( dbFromScratch ) )
                {
                    Assert.That( db.ExecuteScalar( "select Id2 from dbo.tTestVSP where Id = 0" ), Is.EqualTo( 3713 ) );
                }
            }
            finally
            {
                TestHelper.LogToConsole = false;
            }
        }
    }
}
