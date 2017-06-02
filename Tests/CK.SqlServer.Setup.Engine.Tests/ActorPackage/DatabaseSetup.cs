using System;
using System.Data;
using System.Data.SqlClient;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using System.Diagnostics;
using System.Reflection;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    [TestFixture]
    [Category( "DBSetup" )]
    public partial class DatabaseSetup
    {
        [Test]
        public void InstallActorBasic()
        {
            InstallDropAndReverseInstall( false, false, "InstallActorBasic", false );
        }

        [Test]
        public void InstallActorBasicFromScracthDropAndReverseInstall()
        {
            InstallDropAndReverseInstall( true, false, "InstallActorBasicFromScracth" );
        }

        [Test]
        public void InstallActorBasicDropAndReverseInstall()
        {
            InstallDropAndReverseInstall( false, false, "InstallActorBasic" );
        }

        [Test]
        public void InstallActorWithZone()
        {
            InstallDropAndReverseInstall( false, true, "InstallActorWithZone" );
        }

        private static void InstallDropAndReverseInstall( bool resetFirst, bool withZone, string dllName, bool doRevert = true )
        {
            var c = new SetupEngineConfiguration();
            c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverRecurseAssemblyNames.Add( "SqlActorPackage" );
            if( withZone ) c.StObjEngineConfiguration.BuildAndRegisterConfiguration.Assemblies.DiscoverAssemblyNames.Add( "SqlZonePackage" );
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = dllName;
            c.TraceDependencySorterInput = true;
            c.TraceDependencySorterOutput = true;
            var config = new SqlSetupAspectConfiguration();
            config.DefaultDatabaseConnectionString = TestHelper.DatabaseTestConnectionString;
            c.Aspects.Add( config );

            using( var db = SqlManager.OpenOrCreate( TestHelper.DatabaseTestConnectionString, TestHelper.Monitor ) )
            {
                if( resetFirst )
                {
                    db.SchemaDropAllObjects( "bad schema name", true );
                    db.SchemaDropAllObjects( "CK", true );
                    db.SchemaDropAllObjects( "CKCore", false );
                }
            }
            Assert.That(StObjContextRoot.Build(c, null, TestHelper.Monitor));

            using( var db = SqlManager.OpenOrCreate( TestHelper.DatabaseTestConnectionString, TestHelper.Monitor ) )
            {
                var a = Assembly.Load( new AssemblyName( dllName ) );
                IStObjMap m = StObjContextRoot.Load( a, StObjContextRoot.DefaultStObjRuntimeBuilder, TestHelper.Monitor );
                if( withZone ) CheckBasicAndZone( db, m );
                else CheckBasicOnly( db, m );
            }

            if( !doRevert ) return;

            using( var db = SqlManager.OpenOrCreate( TestHelper.DatabaseTestConnectionString, TestHelper.Monitor ) )
            {
                Assert.That( db.ExecuteScalar( "select count(*) from sys.tables where name in ('tActor','tItemVersionStore')" ), Is.EqualTo( 2 ) );
                db.SchemaDropAllObjects( "bad schema name", true );
                db.SchemaDropAllObjects( "CK", true );
                db.SchemaDropAllObjects( "CKCore", false );
                Assert.That( db.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tItemVersionStore')" ), Is.EqualTo( 0 ) );
            }
            c.RunningMode = SetupEngineRunningMode.RevertNames;
            c.StObjEngineConfiguration.FinalAssemblyConfiguration.AssemblyName = dllName + ".Reverted";
            using( TestHelper.Monitor.OpenTrace().Send( "Second setup (reverse order)" ) )
            {
                Assert.That(StObjContextRoot.Build(c, null, TestHelper.Monitor));
            }

            using( var db = SqlManager.OpenOrCreate( TestHelper.DatabaseTestConnectionString, TestHelper.Monitor ) )
            {
                var a = Assembly.Load( new AssemblyName( dllName + ".Reverted" ) );
                IStObjMap m = StObjContextRoot.Load( a, null, TestHelper.Monitor );
                if( withZone ) CheckBasicAndZone( db, m );
                else CheckBasicOnly( db, m );
            }
        }

        private static void CheckBasicOnly( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Monitor.OpenTrace().Send( "CheckBasicOnly" ) )
            {
                Assert.That( c.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.ExecuteScalar( "select count(*) from CK.tGroup where GroupName = 'Public'" ), Is.EqualTo( 1 ) );
                Assert.That( CallExistsUser( c, map, Guid.NewGuid().ToString() ), Is.False );

                int idUInt = CallCreateUser( c, map, "1020" );
                bool result =  CallExistsUser2( c, map, 10, 20 );
                Assert.That( result, Is.True );
                Assert.That( CallExistsUser2( c, map, 10, 25 ), Is.False );

                int idAlbert = CallCreateUser( c, map, "Albert" );
                Assert.That( CallExistsUser( c, map, "Albert" ), Is.True );
                CallCreateGroupBasic( c, map, "BasicGroup" );

                Guid? inAndOut = Guid.NewGuid();
                Assert.That( CallGuidRefTest( c, map, null, ref inAndOut, manualImplementation: false ), Is.EqualTo( "@InOnly is null, @InAndOut is not null." ) );
                Assert.That( inAndOut, Is.Null );

                CheckSqlCallContext( c, map );
                CheckCommandWrapper( c, map );
                CheckCommandParamInjection( c, map );
            }
        }

        private static void CheckBasicAndZone( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Monitor.OpenTrace().Send( "CheckBasicAndZone" ) )
            {
                Assert.That( c.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.ExecuteScalar( "select count(*) from CK.tSecurityZone where SecurityZoneId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.ExecuteScalar( "select count(*) from CK.a_stupid_view" ), Is.GreaterThan( 1 ) );
                CallCreateUser( c, map, Guid.NewGuid().ToString() );
                CallCreateGroupZone( c, map, 0, "ZoneGroupIn0" );
                CallCreateGroupZone( c, map, 1, "ZoneGroupIn1" );
                CallDemoCreateGroup( c, map, "DemoCreate" );
            }
        }

        static string CallGuidRefTest( SqlManager c, IStObjMap map, Guid? inOnly, ref Guid? inAndOut, bool manualImplementation = false )
        {
            var actorHome = map.Default.Obtain<SqlActorPackage.Basic.ActorHome>();
            string text;
            SqlCommand cmd = null;
            if( manualImplementation )
            {
                actorHome.ManualCmdGuidRefTest( ref cmd, inOnly, ref inAndOut, out text );
            }
            else
            {
                actorHome.CmdGuidRefTest( ref cmd, inOnly, ref inAndOut, out text );
            }
            cmd.Connection = c.Connection;
            cmd.ExecuteNonQuery();

            object o = cmd.Parameters["@InAndOut"].Value;
            inAndOut = o == DBNull.Value ? null : (Guid?)o;

            text = (string)cmd.Parameters["@TextResult"].Value;
            cmd.Dispose();
            return text;
        }

        static bool CallExistsUser( SqlManager c, IStObjMap map, string name )
        {
            var userHome = map.Default.Obtain<SqlActorPackage.Basic.UserHome>();
            bool exists;
            SqlCommand cmd = null;
            userHome.CmdExists( ref cmd, name, out exists );
            cmd.Connection = c.Connection;
            cmd.ExecuteNonQuery();
            exists = (bool)cmd.Parameters["@ExistsResult"].Value;
            cmd.Dispose();
            return exists;
        }

        static bool CallExistsUser2( SqlManager c, IStObjMap map, int userPart1, int userPart2 )
        {
            var userHome = map.Default.Obtain<SqlActorPackage.Basic.UserHome>();
            bool exists = true;
            SqlCommand cmd = null;
            //CmdExists2( ref cmd, userPart1, userPart2, out exists );
            userHome.CmdExists2( ref cmd, userPart1, userPart2, out exists );
            cmd.Connection = c.Connection;
            cmd.ExecuteNonQuery();
            exists = (bool)cmd.Parameters["@ExistsResult"].Value;
            cmd.Dispose();
            return exists;
        }

        static void CmdExists2( ref SqlCommand commandRef1, int num1, int num2, out bool flagRef1 )
        {
            SqlParameterCollection parameters;
            SqlCommand command = commandRef1;
            if( command != null )
            {
                parameters = command.Parameters;
                flagRef1 = new bool();
            }
            else
            {
                parameters = (command = dbCKsUserExists2()).Parameters;
                flagRef1 = new bool();
            }
            parameters[0].Value = num1;
            parameters[1].Value = num2;
            parameters[2].Value = (bool)flagRef1;
            commandRef1 = command;
        }


        internal static SqlCommand dbCKsUserExists2()
        {
            SqlCommand command = new SqlCommand( "CK.sUserExists2" )
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            SqlParameterCollection parameters = command.Parameters;
            SqlParameter parameter = new SqlParameter( "@UserPart1", SqlDbType.Int );
            parameters.Add( parameter );
            parameter = new SqlParameter( "@UserPart2", SqlDbType.Int );
            parameters.Add( parameter );
            parameter = new SqlParameter( "@ExistsResult", SqlDbType.Bit );
            parameters.Add( parameter );
            return command;
        }


        static int CallCreateUser( SqlManager c, IStObjMap map, string name )
        {
            var userHome = map.Default.Obtain<SqlActorPackage.Basic.UserHome>();
            int userId;
            using( SqlCommand cmd = userHome.CmdCreate( name, out userId ) )
            {
                cmd.Connection = c.Connection;
                cmd.ExecuteNonQuery();
                userId = (int)cmd.Parameters["@UserIdResult"].Value;
            }
            Assert.That( userId, Is.GreaterThan( 1 ) );
            return userId;
        }

        static int CallCreateGroupBasic( SqlManager c, IStObjMap map, string groupName )
        {
            var groupHome = map.Default.Obtain<SqlActorPackage.Basic.GroupHome>();
            int groupId;
            using( var ctx = new SqlStandardCallContext() )
            {
                groupHome.CmdCreate( ctx, Guid.NewGuid().ToString(), out groupId );
            }
            Assert.That( groupId, Is.GreaterThan( 1 ) );
            return groupId;
        }

        static int CallDemoCreateGroup( SqlManager c, IStObjMap map, string groupName )
        {
            SqlCommand cmd = null;

            var groupHome = map.Default.Obtain<SqlZonePackage.Zone.GroupHome>();

            int groupId;
            groupHome.CmdDemoCreate( ref cmd, 1, groupName );
            cmd.Connection = c.Connection;
            cmd.ExecuteNonQuery();
            // The SqlParameter still exists in the command, even if it is not explicitly declared.
            groupId = (int)cmd.Parameters["@GroupIdResult"].Value;
            Assert.That( groupId, Is.GreaterThan( 1 ) );


            int groupId2;
            groupHome.CmdDemoCreate( ref cmd, 1, groupName + "2" );
            cmd.ExecuteNonQuery();
            // The SqlParameter still exists in the command, even if it is not explicitly declared.
            groupId2 = (int)cmd.Parameters["@GroupIdResult"].Value;
            Assert.That( groupId2, Is.GreaterThan( groupId ) );

            cmd.Dispose();

            return groupId;
        }

        static int CallCreateGroupZone( SqlManager c, IStObjMap map, int securityZoneId, string groupName )
        {
            var groupHome = map.Default.Obtain<SqlZonePackage.Zone.GroupHome>();
            int groupId;
            using( SqlCommand cmd = groupHome.CmdCreate( securityZoneId, groupName.ToString(), out groupId ) )
            {
                cmd.Connection = c.Connection;
                cmd.ExecuteNonQuery();
                groupId = (int)cmd.Parameters["@GroupIdResult"].Value;
            }
            Assert.That( groupId, Is.GreaterThan( 1 ) );
            return groupId;
        }

    }
}
