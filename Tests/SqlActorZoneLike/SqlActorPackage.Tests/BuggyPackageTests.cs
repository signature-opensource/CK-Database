using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System;
using Microsoft.Data.SqlClient;
using System.IO;
using CK.Testing;
using CK.Setup;
using static CK.Testing.SqlServerTestHelper;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class BuggyPackageTests
    {
        static readonly string _configFile;

        static BuggyPackageTests()
        {
            // This file MUST NOT be "shared" by different running test assemblies.
            // "dotnet test" runs the test assemblies in parallel, such files must simply
            // be in the AppContext.BaseDirectory or the TestHelper.TestProjectFolder.
            // But here, this file is read by the SUT project (that doesn't depend on the TestHelper)
            // so we use the AppContext.BaseDirectory here.
            _configFile = Path.Combine( AppContext.BaseDirectory, "BuggyPackageDriver.xml" );
        }

        [Test]
        public void SettleContent_worked()
        {
            if( File.Exists( _configFile ) ) File.Delete( _configFile );

            var p = SharedEngine.Map.StObjs.Obtain<BuggyPackage>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var lastSetup = (DateTime)ctx[p.Database].ExecuteScalar( new SqlCommand( "select LastStartDate from CKCore.tSetupMemory where SurrogateId=0" ) );
                var times = p.ReadSettleContentInfo( ctx );
                times[0].SetupTime.Should().Be( lastSetup );
            }
        }


        [Test]
        public void failing_db_setup_does_not_execute_SettleContent()
        {
            try
            {
                using( TestHelper.Monitor.OpenInfo( "failing_db_setup_does_not_execute_SettleContent (1/2)" ) )
                {
                    File.WriteAllText( _configFile, @"<Error ErrorStep=""Install"" />" );
                    SharedEngine.Reset();
                    SharedEngine.EngineResult.Status.Should().Be( RunStatus.Failed );
                    using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
                    {
                        var con = ctx[TestHelper.GetConnectionString()];
                        var lastSetup = (DateTime)con.ExecuteScalar( new SqlCommand( "select LastStartDate from CKCore.tSetupMemory where SurrogateId=0" ) );
                        var times = BuggyPackage.ReadSettleContentInfo( con );
                        if( times.Count > 0 ) times[0].SetupTime.Should().BeBefore( lastSetup );
                    }
                }
                // Removed config file and runs a new setup.
                File.Delete( _configFile );

                using( TestHelper.Monitor.OpenInfo( "failing_db_setup_does_not_execute_SettleContent (2/2)" ) )
                {
                    SharedEngine.Reset();
                    var p = SharedEngine.Map.StObjs.Obtain<BuggyPackage>();
                    using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
                    {
                        var lastSetup = (DateTime)ctx[p.Database].ExecuteScalar( new SqlCommand( "select LastStartDate from CKCore.tSetupMemory where SurrogateId=0" ) );
                        var times = p.ReadSettleContentInfo( ctx );
                        times[0].SetupTime.Should().Be( lastSetup );
                    }
                }
            }
            finally
            {
                if( File.Exists( _configFile ) ) File.Delete( _configFile );
            }
        }
    }

}
