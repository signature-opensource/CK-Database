using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using FluentAssertions;

namespace CK.SqlServer.Setup.Engine.Tests.Core
{
    [TestFixture]
    public class SqlManagerTests
    {
        static readonly string ConnectionString = TestHelper.GetConnectionString( "INVALID-NAME-TEST" );

        [Test]
        public void SqlManager_OpenOrCreate_catch_any_errors_when_a_Monitor_is_set()
        {
            using( SqlManager m = new SqlManager( TestHelper.Monitor ) )
            {
                Assert.That( m.OpenFromConnectionString( ConnectionString, true ), Is.False );
            }
        }

        [Test]
        public void an_invalid_database_name_does_not_DBSetup_master()
        {
            var c = new StObjEngineConfiguration();
            c.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( "SqlActorPackage" );
            c.FinalAssemblyConfiguration.GenerateFinalAssemblyOption = BuilderFinalAssemblyConfiguration.GenerateOption.DoNotGenerateFile;

            var setupable = new SetupableAspectConfiguration();
            c.Aspects.Add( setupable );

            var sql = new SqlSetupAspectConfiguration();
            sql.DefaultDatabaseConnectionString = ConnectionString;
            c.Aspects.Add( sql );

            Assert.That( StObjContextRoot.Build( c, null, TestHelper.Monitor ), Is.False );

            using( var db = new SqlConnection( TestHelper.ConnectionStringMaster ) )
            {
                db.Open();
                using( var cmd = new SqlCommand( "select DB_Name()", db ) )
                {
                    cmd.ExecuteScalar().Should().Be( "master" );
                    cmd.CommandText = "select count(*) from sys.tables where name = 'tSystem';";
                    cmd.ExecuteScalar().Should().Be( 0 );
                }
            }
        }

        [Test]
        public void an_auto_created_database_does_not_leave_opened_connections()
        {
            string autoTest = TestHelper.GetConnectionString( "TEST_AUTOCREATE" );

            using( var removal = new SqlManager( TestHelper.Monitor ) )
            {
                removal.OpenFromConnectionString( TestHelper.ConnectionStringMaster );
                Assert.That( removal.ExecuteScalar(
                                        @"if db_id('TEST_AUTOCREATE') is not null drop database TEST_AUTOCREATE; 
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
            }

            using( var creator = new SqlManager( TestHelper.Monitor ) )
            {
                Assert.That( creator.OpenFromConnectionString( autoTest, true ) );
                Assert.That( creator.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 1 ) );
            }

            using( var removal = new SqlManager( TestHelper.Monitor ) )
            {
                using( var c = new SqlConnection( autoTest ) )
                {
                    SqlConnection.ClearPool( c );
                }
                removal.OpenFromConnectionString( TestHelper.ConnectionStringMaster );
                Assert.That( removal.ExecuteScalar(
                                        @"if db_id('TEST_AUTOCREATE') is not null drop database TEST_AUTOCREATE; 
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
                Assert.That( removal.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 0 ) );
            }

        }

    }
}
