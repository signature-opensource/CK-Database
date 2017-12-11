#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.SqlServer.Setup.Engine.Tests\Core\ErrorHandlingTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

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

namespace CK.SqlServer.Setup.Engine.Tests
{
    [TestFixture]
    public class ErrorHandlingTests
    {
        static bool _installedDone;

        static public SqlManager CreateInstallContext()
        {
            SqlManager m = new SqlManager( TestHelper.Monitor );
            Assert.That( m.OpenFromConnectionString( TestHelper.DatabaseTestConnectionString, true ), "Unable to open or create CKSqlServerTests database on local server." );
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
                        Assert.That( () => m.ExecuteOneScript( s, null ), Throws.Nothing, s );
                    }
                    string error = (string)m.ExecuteScalar( "select Error from CKCoreTests.tTestErrorLogTestResult" );
                    Assert.That( error, Is.EqualTo( String.Empty ), "No micro test should set an error." );
                }
            }
        }

    }
}
