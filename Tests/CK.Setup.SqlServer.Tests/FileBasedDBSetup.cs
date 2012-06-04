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
                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                c.DiscoverSqlFiles( TestHelper.GetScriptsFolder( "InstallFromScratch" ) );
                Assert.That( c.Run() );
            }
        }

        [Test]
        public void InstallMKS()
        {
            using( var context = new SqlSetupContext( "Server=.;Database=MKSM;Integrated Security=SSPI;", TestHelper.Logger ) )
            {
                if( !context.DefaultDatabase.IsOpen() ) context.DefaultDatabase.OpenOrCreate( ".", "MKSM" );
                SqlSetupCenter c = new SqlSetupCenter( context );
                c.DiscoverFilePackages( TestHelper.GetMKSScriptsFolder( "" ) );
                c.DiscoverSqlFiles( TestHelper.GetMKSScriptsFolder( "" ) );
                Assert.That( c.Run() );
            }
        }
    }
}
