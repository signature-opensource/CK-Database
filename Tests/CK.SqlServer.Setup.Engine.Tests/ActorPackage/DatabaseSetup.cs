using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;
using System.Data.SqlClient;

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
            config.SetupConfiguration.AppDomainConfiguration.UseIndependentAppDomain = true;
            config.SetupConfiguration.TypeFilter = typeFilter;
            config.SetupConfiguration.FinalAssemblyConfiguration.AssemblyName = dllName;

            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                if( resetFirst )
                {
                    db.SchemaDropAllObjects( "CK", true );
                    db.SchemaDropAllObjects( "CKCore", false );
                }
                using( var c = new SqlSetupCenter( TestHelper.Logger, config, db ) )
                {
                    //c.StObjDependencySorterHookInput = TestHelper.Trace;
                    //c.StObjDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    //c.SetupDependencySorterHookInput = TestHelper.Trace;
                    //c.SetupDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    Assert.That( c.Run( typeFilter ) );
                    IStObjMap m = StObjContextRoot.Load( dllName, TestHelper.Logger );
                    if( typeFilter == null ) CheckBasicAndZone( db, m );
                    else CheckBasicOnly( db, m );
                }

                Assert.That( db.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tActor','tItemVersion')" ), Is.EqualTo( 2 ) );
                db.SchemaDropAllObjects( "CK", true );
                db.SchemaDropAllObjects( "CKCore", false );
                Assert.That( db.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tItemVersion')" ), Is.EqualTo( 0 ) );

                using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "Second setup (reverse order)" ) )
                {
                    config.SetupConfiguration.RevertOrderingNames = true;
                    using( var c = new SqlSetupCenter( TestHelper.Logger, config, db ) )
                    {
                        Assert.That( c.Run() );
                        IStObjMap m = StObjContextRoot.Load( dllName, TestHelper.Logger );
                        if( typeFilter == null ) CheckBasicAndZone( db, m );
                        else CheckBasicOnly( db, m );
                    }
                }
            }
        }

        private static void CheckBasicOnly( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "CheckBasicOnly" ) )
            {
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tGroup where GroupName = 'Public'" ), Is.EqualTo( 1 ) );
                CallCreateUser( c, map );
                CallCreateGroupBasic( c, map, "BasicGroup" );
            }
        }

        private static void CheckBasicAndZone( SqlManager c, IStObjMap map )
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Trace, "CheckBasicAndZone" ) )
            {
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
                Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tSecurityZone where SecurityZoneId <= 1" ), Is.EqualTo( 2 ) );
                CallCreateUser( c, map );
                CallCreateGroupZone( c, map, 0, "ZoneGroupIn0" );
                CallCreateGroupZone( c, map, 1, "ZoneGroupIn1" );
                CallDemoCreateGroup( c, map, "DemoCreate" );
            }
        }

        static int CallCreateUser( SqlManager c, IStObjMap map )
        {
            var userHome = map.Default.Obtain<SqlActorPackage.Basic.UserHome>();
            int userId;
            using( SqlCommand cmd = userHome.CmdCreate( Guid.NewGuid().ToString(), out userId ) )
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
