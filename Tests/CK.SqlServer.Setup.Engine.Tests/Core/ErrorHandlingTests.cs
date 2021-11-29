using NUnit.Framework;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using static CK.Testing.DBSetupTestHelper;
using FluentAssertions;

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class ErrorHandlingTests
    {
        static bool _installedDone;

        static public SqlManager CreateInstallContext()
        {
            SqlManager m = new SqlManager( TestHelper.Monitor );
            Assert.That( m.OpenFromConnectionString( TestHelper.GetConnectionString(), true ), "Unable to open or create CKSqlServerTests database on local server." );
            if( !_installedDone )
            {
                m.EnsureCKCoreIsInstalled( TestHelper.Monitor );
                var install = SqlHelper.SplitGoSeparator( File.ReadAllText( Path.Combine( TestHelper.TestProjectFolder, "Scripts/ErrorHandling.Install.sql" ) ) );
                m.ExecuteScripts( install, TestHelper.Monitor );
                _installedDone = true;
            }
            return m;
        }

        [Test]
        public void ErrorHandlingMicroTests()
        {
            using( SqlManager m = CreateInstallContext() )
            {
                var microTests = SqlHelper.SplitGoSeparator( File.ReadAllText( Path.Combine( TestHelper.TestProjectFolder, "Scripts/ErrorHandling.MicroTests.sql" ) ) );
                foreach( string s in microTests.Where( script => script.Contains( "bug" ) ) )
                {
                    bool errorExpected = s.Contains( "EXCEPTION" );
                    if( errorExpected )
                    {
                        // Checks that an exception is raised since there is no monitor.
                        Assert.Throws<SqlException>( () => m.ExecuteOneScript( s, null ), s );
                        // Dump to console.
                        Assert.That( m.ExecuteOneScript( s, TestHelper.Monitor ), Is.False, s );
                    }
                    else
                    {
                        FluentActions.Invoking( () => m.ExecuteOneScript( s, null ) ).Should().NotThrow();
                    }
                    string error = (string)m.ExecuteScalar( "select Error from CKCoreTests.tTestErrorLogTestResult" );
                    Assert.That( error, Is.EqualTo( String.Empty ), "No micro test should set an error." );
                }
            }
        }

    }
}
