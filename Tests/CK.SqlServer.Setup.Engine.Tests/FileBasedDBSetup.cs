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
            using( var context = new SqlSetupContext( SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) ) )
            {
                using( context.Logger.OpenGroup( LogLevel.Trace, "First setup" ) )
                {
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    Assert.That( c.Run() );
                }
                
                context.DefaultSqlDatabase.ExecuteOneScript( "drop procedure Test.sOneStoredProcedure;" );
                context.DefaultSqlDatabase.ExecuteOneScript( "drop function Test.fTest;" );
                
                using( context.Logger.OpenGroup( LogLevel.Trace, "Second setup" ) )
                {
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    Assert.That( c.Run() );
                }
            }
        }

        [Test]
        public void InstallPackageWithView()
        {
            using( var context = new SqlSetupContext( SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) ) )
            {
                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
                c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
                Assert.That( c.Run() );
                Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
            }
        }

        [Test]
        public void InstallPackageWithSPDependsOnVersion()
        {
            using( var context = new SqlSetupContext( SqlManager.OpenOrCreate( ".", "Test", TestHelper.Logger ) ) )
            {
                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tSetupMemoryItem]') is not null delete from [CKCore].[tSetupMemoryItem] where ItemKey like '%WithSPDependsOnVersion%';" );
                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"if object_id(N'[CKCore].[tItemVersion]') is not null delete from [CKCore].[tItemVersion] where FullName like '%WithSPDependsOnVersion%';" );

                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[tTestVSP]') is not null drop table dbo.tTestVSP;" ); // Reset
                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"if object_id(N'[dbo].[sStoredProcedureWithSPDependsOnVersion]') is not null drop procedure [dbo].[sStoredProcedureWithSPDependsOnVersion];" );


                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
                c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
                Assert.That( c.Run() );
                Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select Id2 from dbo.tTestVSP where Id = 0" ), Is.EqualTo( 3713 ) );


            }
        }
    }
}
