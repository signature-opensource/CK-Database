using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;
using System.Data.SqlClient;
using System.Data;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    [TestFixture]
    [Category( "DBSetup" )]
    public class DatabaseSetup
    {
        [Test]
        public void InstallActorBasicFromScracth()
        {
            InstallDropAndReverseInstall( true, t => t.Namespace.StartsWith( "SqlActorPackage.Basic" ), "InstallDropAndReverseInstall" );
        }

        [Test]
        public void InstallActorBasic()
        {
            InstallDropAndReverseInstall( false, t => t.Namespace.StartsWith( "SqlActorPackage.Basic" ), "InstallDropAndReverseInstall" );
        }

        [Test]
        public void InstallActorWithZone()
        {
            InstallDropAndReverseInstall( false, null, "InstallDropAndReverseInstall.WithZone" );
        }

        private static void InstallDropAndReverseInstall( bool resetFirst, Predicate<Type> typeFilter, string dllName )
        {
            var config = new SqlSetupCenterConfiguration();
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "SqlActorPackage" );
            config.SetupConfiguration.AppDomainConfiguration.Assemblies.DiscoverAssemblyNames.Add( "SqlZonePackage" );
            config.SetupConfiguration.TypeFilter = typeFilter;
            config.SetupConfiguration.FinalAssemblyConfiguration.AssemblyName = dllName;
            config.SetupConfiguration.AppDomainConfiguration.UseIndependentAppDomain = true;
            config.SetupConfiguration.AppDomainConfiguration.ProbePaths.Add( TestHelper.TestBinFolder );

            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                if( resetFirst )
                {
                    db.SchemaDropAllObjects( "CK", true );
                    db.SchemaDropAllObjects( "CKCore", false );
                }
                config.DefaultDatabaseConnectionString = db.CurrentConnectionString;
            }
            //
            //using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            //using( var c = new SqlSetupCenter( TestHelper.Logger, config, db ) )
            //{
            //    //c.StObjDependencySorterHookInput = TestHelper.Trace;
            //    //c.StObjDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
            //    //c.SetupDependencySorterHookInput = TestHelper.Trace;
            //    //c.SetupDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
            //    Assert.That( c.Run() );
            //    IStObjMap m = StObjContextRoot.Load( dllName, TestHelper.Logger );
            //    if( typeFilter == null ) CheckBasicAndZone( db, m );
            //    else CheckBasicOnly( db, m );
            //}
            // 
            // The code above explicitely creates a SqlSetupCenter and Run() it.
            // This executes the build process directly, in the current domain.
            // To honor SetupConfiguration.AppDomainConfiguration.UseIndependentAppDomain, 
            // the static StObjContext.Build with the configuration must be used as below:
            // 
            // StObjContextRoot.Build result must be disposed: this actually unloads 
            // the independant AppDomain from memory.
            //
            using( var result = StObjContextRoot.Build( config, TestHelper.Logger ) )
            {
                Assert.That( result.Success );
                Assert.That( result.IndependentAppDomain != null );
            }
            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                IStObjMap m = StObjContextRoot.Load( dllName, TestHelper.Logger );
                if( typeFilter == null ) CheckBasicAndZone( db, m );
                else CheckBasicOnly( db, m );
            }
            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                Assert.That( db.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tActor','tItemVersion')" ), Is.EqualTo( 2 ) );
                db.SchemaDropAllObjects( "CK", true );
                db.SchemaDropAllObjects( "CKCore", false );
                Assert.That( db.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tItemVersion')" ), Is.EqualTo( 0 ) );
            }
             
            config.SetupConfiguration.RevertOrderingNames = true;
            using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "Second setup (reverse order)" ) )
            {
                using( var result = StObjContextRoot.Build( config, TestHelper.Logger ) )
                {
                    Assert.That( result.Success );
                    Assert.That( result.IndependentAppDomain != null );
                }
            }

            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                IStObjMap m = StObjContextRoot.Load( dllName, TestHelper.Logger );
                if( typeFilter == null ) CheckBasicAndZone( db, m );
                else CheckBasicOnly( db, m );
            }
        }

        private static void CheckBasicOnly( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "CheckBasicOnly" ) )
            {
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tGroup where GroupName = 'Public'" ), Is.EqualTo( 1 ) );
                Assert.That( CallExistsUser( c, map, Guid.NewGuid().ToString() ), Is.False );
                
                int idUInt = CallCreateUser( c, map, "1020" );
                bool result =  CallExistsUser2( c, map, 10, 20 );
                Assert.That( result, Is.True );
                Assert.That( CallExistsUser2( c, map, 10, 25 ), Is.False );

                int idAlbert = CallCreateUser( c, map, "Albert" );
                Assert.That( CallExistsUser( c, map, "Albert" ), Is.True );
                CallCreateGroupBasic( c, map, "BasicGroup" );

                Guid? inAndOut = Guid.NewGuid();
                Assert.That( CallGuidRefTest( c, map, null, ref inAndOut, manualImplementation:false ), Is.EqualTo( "@InOnly is null, @InAndOut is not null." ) );
                Assert.That( inAndOut, Is.Null );
            }
        }

        private static void CheckBasicAndZone( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "CheckBasicAndZone" ) )
            {
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tSecurityZone where SecurityZoneId <= 1" ), Is.EqualTo( 2 ) );
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
            c.Connection.ExecuteNonQuery( cmd );
            
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
            c.Connection.ExecuteNonQuery( cmd );
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
            c.Connection.ExecuteNonQuery( cmd );
            exists = (bool)cmd.Parameters["@ExistsResult"].Value;
            cmd.Dispose();
            return exists;
        }

        static void CmdExists2(ref SqlCommand commandRef1, int num1, int num2, out bool flagRef1)
        {
            SqlParameterCollection parameters;
            SqlCommand command = commandRef1;
            if (command != null)
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
            parameters[2].Value = (bool) flagRef1;
            commandRef1 = command;
        }


        internal static SqlCommand dbCKsUserExists2()
        {
            SqlCommand command = new SqlCommand("CK.sUserExists2") {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            SqlParameterCollection parameters = command.Parameters;
            SqlParameter parameter = new SqlParameter("@UserPart1", SqlDbType.Int);
            parameters.Add(parameter);
            parameter = new SqlParameter("@UserPart2", SqlDbType.Int);
            parameters.Add(parameter);
            parameter = new SqlParameter("@ExistsResult", SqlDbType.Bit);
            parameters.Add(parameter);
            return command;
        }


        static int CallCreateUser( SqlManager c, IStObjMap map, string name )
        {
            var userHome = map.Default.Obtain<SqlActorPackage.Basic.UserHome>();
            int userId;
            using( SqlCommand cmd = userHome.CmdCreate( name, out userId ) )
            {
                c.Connection.ExecuteNonQuery( cmd );
                userId = (int)cmd.Parameters["@UserIdResult"].Value;
            }
            Assert.That( userId, Is.GreaterThan( 1 ) );
            return userId;
        }

        static int CallCreateGroupBasic( SqlManager c, IStObjMap map, string groupName )
        {
            var groupHome = map.Default.Obtain<SqlActorPackage.Basic.GroupHome>();
            int groupId;
            using( SqlCommand cmd = groupHome.CmdCreate( Guid.NewGuid().ToString(), out groupId ) )
            {
                c.Connection.ExecuteNonQuery( cmd );
                groupId = (int)cmd.Parameters["@GroupIdResult"].Value;
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
            c.Connection.ExecuteNonQuery( cmd );
            // The SqlParameter still exists in the command, even if it is not explicitely declared.
            groupId = (int)cmd.Parameters["@GroupIdResult"].Value;
            Assert.That( groupId, Is.GreaterThan( 1 ) );


            int groupId2;
            groupHome.CmdDemoCreate( ref cmd, 1, groupName+"2" );
            c.Connection.ExecuteNonQuery( cmd );
            // The SqlParameter still exists in the command, even if it is not explicitely declared.
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
                c.Connection.ExecuteNonQuery( cmd );
                groupId = (int)cmd.Parameters["@GroupIdResult"].Value;
            }
            Assert.That( groupId, Is.GreaterThan( 1 ) );
            return groupId;
        }

    }
}
