using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using CK.SqlServer;

namespace CK.Setup.SqlServer.Tests.ActorPackage
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
                }
            }
        }
    }
}
