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
    }
}
