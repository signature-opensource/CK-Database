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
                if( !context.DefaultDatabase.IsOpen() ) context.DefaultDatabase.OpenOrCreate( ".", "Test" );
                using( context.Logger.OpenGroup( LogLevel.Trace, "First setup" ) )
                {
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                    Assert.That( c.Run() );
                }
                
                context.DefaultDatabase.ExecuteOneScript( "drop procedure Test.sOneStoredProcedure;" );
                context.DefaultDatabase.ExecuteOneScript( "drop function Test.fTest;" );
                
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
                if( !context.DefaultDatabase.IsOpen() ) context.DefaultDatabase.OpenOrCreate( @".", "Test" );
                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
                c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratchWithView" ) );
                Assert.That( c.Run() );
                Assert.That( context.DefaultDatabase.Connection.ExecuteScalar( "select Id from dbo.vTestView" ), Is.EqualTo( 3712 ) );
            }
        }
    }
}
