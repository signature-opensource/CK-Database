using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using SqlActorPackage.Basic;
using System;
using System.Data.SqlClient;
using System.IO;
using static CK.Testing.CKDatabaseLocalTestHelper;

namespace SqlActorPackage.Tests
{
    [TestFixture]
    public class BuggyPackageTests
    {
        static readonly string _configFile;

        static BuggyPackageTests()
        {
            _configFile = TestHelper.TestProjectFolder
                            .Combine( "../BasicModels/SqlActorPackage.Runtime/BuggyPackageDriver.xml" )
                            .ResolveDots();
        }

        [Test]
        public void SettleContent_worked()
        {
            if( File.Exists( _configFile ) ) File.Delete( _configFile );

            var p = TestHelper.StObjMap.Default.Obtain<BuggyPackage>();
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
            File.WriteAllText( _configFile, @"<Error ErrorStep=""Install"" />" );
            TestHelper.ResetStObjMap();
            TestHelper.RunDBSetup().Should().Be( CKSetup.CKSetupRunResult.Failed );
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var con = ctx[TestHelper.GetConnectionString()];
                var lastSetup = (DateTime)con.ExecuteScalar( new SqlCommand( "select LastStartDate from CKCore.tSetupMemory where SurrogateId=0" ) );
                var times = BuggyPackage.ReadSettleContentInfo( con );
                if( times.Count > 0 ) times[0].SetupTime.Should().BeBefore( lastSetup );
            }
            // Removed config file and runs a new setup.
            File.Delete( _configFile );

            var map = TestHelper.StObjMap;
            map.Should().NotBeNull();
            var p = map.Default.Obtain<BuggyPackage>();
            using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var lastSetup = (DateTime)ctx[p.Database].ExecuteScalar( new SqlCommand( "select LastStartDate from CKCore.tSetupMemory where SurrogateId=0" ) );
                var times = p.ReadSettleContentInfo( ctx );
                times[0].SetupTime.Should().Be( lastSetup );
            }
        }
    }

}
