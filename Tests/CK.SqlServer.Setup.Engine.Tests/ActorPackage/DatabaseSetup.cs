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
            using( var context = new SqlSetupContext( SqlManager.OpenOrCreate( ".", "ActorPackage", TestHelper.Logger ) ) )
            {
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "SqlActorPackage" );
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "SqlZonePackage" );
                context.AssemblyRegistererConfiguration.DiscoverAssemblyNames.Add( "CK.Authentication.Local" );
                using( context.Logger.OpenGroup( LogLevel.Trace, "First setup" ) )
                {
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.StObjDependencySorterHookInput = TestHelper.Trace;
                    c.StObjDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    c.SetupDependencySorterHookInput = TestHelper.Trace;
                    c.SetupDependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
                    Assert.That( c.Run( typeFilter ) );
                    if( typeFilter == null ) CheckBasicAndZone( context.DefaultSqlDatabase );
                    else CheckBasicOnly( context.DefaultSqlDatabase );
                }

                Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tActor','tItemVersion')" ), Is.EqualTo( 2 ) );
                context.DefaultSqlDatabase.SchemaDropAllObjects( "CK", true );
                context.DefaultSqlDatabase.SchemaDropAllObjects( "CKCore", false );
                Assert.That( context.DefaultSqlDatabase.Connection.ExecuteScalar( "select count(*) from sys.tables where name in ('tSystem','tItemVersion')" ), Is.EqualTo( 0 ) );

                using( context.Logger.OpenGroup( LogLevel.Trace, "Second setup" ) )
                {
                    SqlSetupCenter c = new SqlSetupCenter( context );
                    c.RevertOrderingNames = true;
                    Assert.That( c.Run( typeFilter ) );
                    if( typeFilter == null ) CheckBasicAndZone( context.DefaultSqlDatabase );
                    else CheckBasicOnly( context.DefaultSqlDatabase );
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
