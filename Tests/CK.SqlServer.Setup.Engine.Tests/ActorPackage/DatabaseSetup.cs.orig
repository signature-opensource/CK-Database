using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;

namespace CK.SqlServer.Setup.Engine.Tests.ActorPackage
{
    [TestFixture]
    public class DatabaseSetup
    {
        [Test]
        public void InstallBasic()
        {
            InstallDropAndReverseInstall( t => t.Namespace.StartsWith( "SqlActorPackage.Basic" ) );
        }

        [Test]
        public void InstallZone()
        {
            InstallDropAndReverseInstall( null );
        }

        private static void InstallDropAndReverseInstall( Predicate<Type> typeFilter )
        {
            var config = new SqlSetupCenterConfiguration();
            config.SetupConfiguration.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "SqlActorPackage" );
            config.SetupConfiguration.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "SqlZonePackage" );
            config.SetupConfiguration.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "CK.Authentication.Local" );
            config.SetupConfiguration.TypeFilter = typeFilter;
            config.SetupConfiguration.StObjFinalAssemblyConfiguration.AssemblyName = "InstallDropAndReverseInstall";

            using( var db = SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) )
            {
                using( var c = new SqlSetupCenter( TestHelper.Logger, config, db ) )
                {
                    c.StObjDependencySorterHookInput = TestHelper.Trace;
                    c.StObjDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    c.SetupDependencySorterHookInput = TestHelper.Trace;
                    c.SetupDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    Assert.That( c.Run( typeFilter ) );
                    if( typeFilter == null ) CheckBasicAndZone( db );
                    else CheckBasicOnly( db );
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
                        if( typeFilter == null ) CheckBasicAndZone( db );
                        else CheckBasicOnly( db );
                    }
                }
            }
        }

        private static void CheckBasicAndZone( SqlManager c )
        {
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tSecurityZone where SecurityZoneId <= 1" ), Is.EqualTo( 2 ) );
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tUser where UserId <= 1" ), Is.EqualTo( 2 ) );
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tGroup where SecurityZoneId <= 1" ), Is.EqualTo( 2 ) );
        }

        private static void CheckBasicOnly( SqlManager c )
        {
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tActor where ActorId <= 1" ), Is.EqualTo( 2 ) );
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tGroup where GroupName = 'Public'" ), Is.EqualTo( 1 ) );
            Assert.That( c.Connection.ExecuteScalar( "select count(*) from CK.tUser where UserId <= 1" ), Is.EqualTo( 2 ) );
        }
    }
}
