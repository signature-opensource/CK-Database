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
    public class FileBasedDBSetup
    {
        [Test]
        public void InstallFromScratch()
        {
            SqlSetupCenterConfiguration config = new SqlSetupCenterConfiguration();
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
            config.SetupConfiguration.StObjFinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) )
            {
                using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "First setup" ) )
                {
                    using( SqlSetupCenter c = new SqlSetupCenter( TestHelper.Logger, config, defaultDB ) )
                    {
                        Assert.That( c.Run() );
                    }
                }

                defaultDB.ExecuteOneScript( "drop procedure Test.sOneStoredProcedure;" );
                defaultDB.ExecuteOneScript( "drop function Test.fTest;" );

                using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "Second setup" ) )
                {
                    using( SqlSetupCenter c = new SqlSetupCenter( TestHelper.Logger, config, defaultDB ) )
                    {
                        Assert.That( c.Run() );
                    }
                }
            }
        }

        [Test]
        public void InstallPackageWithView()
        {
            SqlSetupCenterConfiguration config = new SqlSetupCenterConfiguration();
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
            config.SetupConfiguration.StObjFinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) )
            {
                using( var c = new SqlSetupCenter( TestHelper.Logger, config, defaultDB ) )
                {
                    Assert.That( c.Run() );
                }
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
            }
        }

        [Test]
        public void InstallPackageWithSPDependsOnVersion()
        {
            SqlSetupCenterConfiguration config = new SqlSetupCenterConfiguration();
            config.FilePackageDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
            config.SqlFileDirectories.Add( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
            config.SetupConfiguration.StObjFinalAssemblyConfiguration.AssemblyName = "InstallPackageWithSPDependsOnVersion";

            using( var defaultDB = SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) )
            {
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tSetupMemoryItem]') is not null delete from [CKCore].[tSetupMemoryItem] where ItemKey like '%WithSPDependsOnVersion%';" );
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tItemVersion]') is not null delete from [CKCore].[tItemVersion] where FullName like '%WithSPDependsOnVersion%';" );

                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[tTestVSP]') is not null drop table dbo.tTestVSP;" ); // Reset
                defaultDB.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[sStoredProcedureWithSPDependsOnVersion]') is not null drop procedure [dbo].[sStoredProcedureWithSPDependsOnVersion];" );

                using( SqlSetupCenter c = new SqlSetupCenter( TestHelper.Logger, config, defaultDB ) )
                {
                    Assert.That( c.Run() );
                }
                Assert.That( defaultDB.Connection.ExecuteScalar( "select Id2 from dbo.tTestVSP where Id = 0" ), Is.EqualTo( 3713 ) );
            }
        }
    }
}
