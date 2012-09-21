using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;

namespace CK.Setup.SqlServer.Tests
{
    [TestFixture]
    public class FileBasedDBSetup
    {
        [Test]
        public void InstallFromScratch()
        {
            using( var context = new SqlSetupContext( "Server=.;Database=Test;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultSqlDatabase.IsOpen() ) context.DefaultSqlDatabase.OpenOrCreate( ".", "Test" );
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
            using( var context = new SqlSetupContext( @"Server=.;Database=Test;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultSqlDatabase.IsOpen() ) context.DefaultSqlDatabase.OpenOrCreate( @".", "Test" );
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
            using( var context = new SqlSetupContext( @"Server=.;Database=Test;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultSqlDatabase.IsOpen() ) context.DefaultSqlDatabase.OpenOrCreate( @".", "Test" );

                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"delete from [CKCore].[tSetupMemoryItem] where ItemKey like '%WithSPDependsOnVersion%';" );
                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"delete from [CKCore].[tItemVersion] where FullName like '%WithSPDependsOnVersion%';" );

                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tTestVSP]') AND type in (N'U')) drop table dbo.tTestVSP;" ); // Reset
                context.DefaultSqlDatabase.Connection.ExecuteNonQuery( @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sStoredProcedureWithSPDependsOnVersion]') AND type in (N'P', N'PC')) DROP PROCEDURE [dbo].[sStoredProcedureWithSPDependsOnVersion];" );


                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
                c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratchWithSPDependsOnVersion" ) );
                Assert.That( c.Run() );
                Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select Id2 from dbo.tTestVSP where Id = 0" ), Is.EqualTo( 3713 ) );


            }
        }
    }
}
