using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CK.Core;

namespace CK.SqlServer.Tests
{
    [TestFixture]
    public class ErrorHandlingTests
    {
        static bool _installedDone;

        static public SqlManager CreateInstallContext()
        {
            SqlManager m = new SqlManager();
            Assert.That( m.OpenOrCreate( ".", "CKSqlServerTests" ), "Unable to open or create CKSqlServerTests database on local server." );
            if( !_installedDone )
            {
                m.EnsureCKCoreIsInstalled( TestHelper.ConsoleMonitor );
                var install = SqlHelper.SplitGoSeparator( File.ReadAllText( TestHelper.GetScriptsFolder( "ErrorHandling.Install.sql" ) ) );
                m.ExecuteScripts( install, TestHelper.ConsoleMonitor );
                _installedDone = true;
            }
            return m;
        }

        [Test]
        public void ErrorHandlingMicroTests()
        {
            using( SqlManager m = CreateInstallContext() )
            {
                var microTests = SqlHelper.SplitGoSeparator( File.ReadAllText( TestHelper.GetScriptsFolder( "ErrorHandling.MicroTests.sql" ) ) );
                foreach( string s in microTests.Where( script => script.Contains( "bug" ) ) )
                {
                    bool errorExpected = s.Contains( "EXCEPTION" );
                    if( errorExpected )
                    {
                        // Checks that an exception is raised since there is no monitor.
                        Assert.Throws<SqlException>( () => m.ExecuteOneScript( s, null ), s );
                        // Dump to console.
                        Assert.That( m.ExecuteOneScript( s, TestHelper.ConsoleMonitor ), Is.False, s );
                    }
                    else
                    {
                        Assert.That( () => m.ExecuteOneScript( s, null ), Throws.Nothing, s );
                    }
                    string error = (string)m.Connection.ExecuteScalar( "select Error from CKCoreTests.tTestErrorLogTestResult" );
                    Assert.That( error, Is.EqualTo( String.Empty ), "No micro test should set an error." );
                }
            }
        }

    }
}
