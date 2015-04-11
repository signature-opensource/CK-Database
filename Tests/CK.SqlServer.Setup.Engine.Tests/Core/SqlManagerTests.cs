using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.SqlServer.Setup.Engine.Tests.Core
{
    [TestFixture]
    public class SqlManagerTests
    {

        const string ConnectionString = "Server=.;Database=INVALID-NAME-TEST;Integrated Security=SSPI";

        [Test]
        public void SqlManager_OpenOrCreate_catch_any_errors_when_a_Monitor_is_set()
        {
            using( SqlManager m = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                Assert.That( m.OpenFromConnectionString( ConnectionString, true ), Is.False );
            }
        }

        [Test]
        public void an_invalid_database_name_does_not_DBSetup_master()
        {
            var c = new SetupEngineConfiguration();
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( "IntoTheWild0" );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.DoNotGenerateFinalAssembly = true;
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );
            var config = new SqlSetupAspectConfiguration();
            c.Aspects.Add( config );
            config.DefaultDatabaseConnectionString = ConnectionString;

            using( var r = StObjContextRoot.Build( c, null, TestHelper.ConsoleMonitor ) )
            {
                Assert.That( r.Success, Is.False );
            }

            using( var defaultDB = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                defaultDB.OpenFromConnectionString( TestHelper.MasterConnection );
                Assert.That( defaultDB.Connection.ExecuteScalar( "select DB_Name()" ), Is.EqualTo( "master" ) );
                Assert.That( defaultDB.Connection.ExecuteScalar( "select count(*) from sys.tables where name = 'tSystem';" ), Is.EqualTo( 0 ) );
            }
        }

        [Test]
        public void an_auto_created_database_does_not_leave_opened_connections()
        {
            string autoTest = "Server=.;Initial Catalog=TEST_AUTOCREATE;Integrated Security=SSPI;";

            using( var removal = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                removal.OpenFromConnectionString( TestHelper.MasterConnection );
                Assert.That( removal.Connection.ExecuteScalar(
                                        @"if db_id('TEST_AUTOCREATE') is not null drop database TEST_AUTOCREATE; 
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
            }

            using( var creator = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                Assert.That( creator.OpenFromConnectionString( autoTest, true ) );
                Assert.That( creator.Connection.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 1 ) );
            }

            //using( var creator = new SqlManager( TestHelper.ConsoleMonitor ) )
            //{
            //    Assert.That( creator.OpenFromConnectionString( autoTest ), Is.False );
            //    //using( SqlConnectionProvider c = new SqlConnectionProvider( autoTest ) )
            //    //{
            //    //    try
            //    //    {
            //    //        c.Open();
            //    //        Assert.Fail( "Database dors not exist." );
            //    //    }
            //    //    catch
            //    //    {
            //    //        // Must Create the database.
            //    //    }
            //    //}
            //    creator.OpenFromConnectionString( TestHelper.MasterConnection );
            //    creator.Connection.ExecuteNonQuery( "create database TEST_AUTOCREATE" );
            //    Assert.That( creator.Connection.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 1 ) );
            //}

            //using( var creator = new SqlManager( TestHelper.ConsoleMonitor ) )
            //{
            //    bool done = false;
            //    if( !creator.OpenFromConnectionString( autoTest ) )
            //    {
            //        string name;
            //        string master = SqlManager.GetMasterConnectionString( autoTest, out name );
            //        using( var m = new SqlConnectionProvider( master ) )
            //        {
            //            m.ExecuteNonQuery( "create database " + name );
            //        }
            //        SqlConnection.ClearPool( new SqlConnection( autoTest ) );
            //        done = creator.OpenFromConnectionString( autoTest );
            //    }
            //    Assert.That( done );
            //    Assert.That( creator.Connection.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 1 ) );
            //}

            using( var removal = new SqlManager( TestHelper.ConsoleMonitor ) )
            {
                using( var c = new SqlConnection( autoTest ) )
                {
                    SqlConnection.ClearPool( c );
                }
                removal.OpenFromConnectionString( TestHelper.MasterConnection );
                Assert.That( removal.Connection.ExecuteScalar(
                                        @"if db_id('TEST_AUTOCREATE') is not null drop database TEST_AUTOCREATE; 
                                          select 'Done';" ), Is.EqualTo( "Done" ) );
                Assert.That( removal.Connection.ExecuteScalar( @"select count(*) from sys.databases where name = 'TEST_AUTOCREATE';" ), Is.EqualTo( 0 ) );
            }

        }

    }
}
