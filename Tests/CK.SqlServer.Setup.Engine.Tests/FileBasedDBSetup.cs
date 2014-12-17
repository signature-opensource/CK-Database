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

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    [Category("DBSetup")]
    public class FileBasedDBSetup
    {
        [Test]
        public void InstallFromScratch()
        {
            SqlSetupAspectConfiguration config = new SqlSetupAspectConfiguration();
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "Test", TestHelper.ConsoleMonitor ) )
            {
                defaultDB.SchemaDropAllObjects( "Test", true );
                defaultDB.SchemaDropAllObjects( "CKCore", false );

                config.DefaultDatabaseConnectionString = defaultDB.CurrentConnectionString;

                using( TestHelper.ConsoleMonitor.OpenTrace().Send( "First setup (will fail due to a MissingDependencyIsError configuration)." ) )
                {
                    using( var r = StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ) )
                    {
                        Assert.That( r.Success, Is.False );
                    }
                }
                config.IgnoreMissingDependencyIsError = true;
                using( TestHelper.ConsoleMonitor.OpenTrace().Send( "Second setup (will succeed since SqlSetupCenterConfiguration.IgnoreMissingDependencyIsError is true)." ) )
                {
                    using( var r = StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ) )
                    {
                        Assert.That( r.Success );
                    }
                }

                defaultDB.ExecuteOneScript( "drop procedure Test.sOneStoredProcedure;" );
                defaultDB.ExecuteOneScript( "drop function Test.fTest;" );

                using( TestHelper.ConsoleMonitor.OpenTrace().Send( "Third setup." ) )
                {
                    using( var r = StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ) )
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
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
            config.SetupConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            config.IgnoreMissingDependencyIsError = true;

            // Ensures that the database is created and gets the connection string.
            using( var defaultDB = SqlManager.OpenOrCreate( ".", "TestWithView", TestHelper.ConsoleMonitor ) )
            {
                config.DefaultDatabaseConnectionString = defaultDB.CurrentConnectionString;
            }

            using( var r = StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ) )
            {
                Assert.That( r.Success );
            }

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "TestWithView", TestHelper.ConsoleMonitor ) )
            {
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
                defaultDB.Connection.ExecuteNonQuery( "drop view dbo.vTestView" );
                defaultDB.Connection.ExecuteNonQuery( "drop table dbo.tTestV" );
                defaultDB.SchemaDropAllObjects( "CKCore", false );
            }
            // From scratch now: the database is empty.

            using( var r = StObjContextRoot.Build( config, null, TestHelper.ConsoleMonitor ) )
            {
                Assert.That( r.Success );
            }

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "TestWithView", TestHelper.ConsoleMonitor ) )
            {
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
            }
        }

        [Test]
        public void InstallPackageWithSPDependsOnVersion()
        {
            SqlSetupAspectConfiguration config = new SqlSetupAspectConfiguration();
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
            config.SetupConfiguration.FinalAssemblyConfiguration.AssemblyName = "InstallPackageWithSPDependsOnVersion";

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "Test", TestHelper.ConsoleMonitor ) )
            {
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tSetupMemoryItem]') is not null delete from [CKCore].[tSetupMemoryItem] where ItemKey like '%WithSPDependsOnVersion%';" );
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tItemVersion]') is not null delete from [CKCore].[tItemVersion] where FullName like '%WithSPDependsOnVersion%';" );

                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[tTestVSP]') is not null drop table dbo.tTestVSP;" ); // Reset
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[sStoredProcedureWithSPDependsOnVersion]') is not null drop procedure [dbo].[sStoredProcedureWithSPDependsOnVersion];" );

                using( SqlSetupAspect c = new SqlSetupAspect( TestHelper.ConsoleMonitor, config, StObjContextRoot.DefaultStObjRuntimeBuilder, defaultDB ) )
                {
                    Assert.That( c.Center.Run() );
                }
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id2 from dbo.tTestVSP where Id = 0" ), Is.EqualTo( 3713 ) );
            }
        }
    }
}
