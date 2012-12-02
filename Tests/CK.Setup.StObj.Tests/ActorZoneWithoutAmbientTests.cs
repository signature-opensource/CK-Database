using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;
using CK.Setup.StObj.Tests.SimpleObjects;

namespace CK.Setup.StObj.Tests
{
    [TestFixture]
    [CLSCompliant(false)]
    public class ActorZoneWithoutAmbientTests
    {
        [StObj( ItemKind = DependentItemKind.Group, 
                Children = new Type[]{ typeof(BasicPackage), typeof(BasicActor), typeof(BasicGroup), typeof(ZonePackage), typeof(ZoneGroup), typeof(SecurityZone)} )]
        class SqlDatabaseDefault : IAmbientContract
        {
        }

        [StObj( ItemKind = DependentItemKind.Container )]
        class BasicPackage : IAmbientContract
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKind.Item )]
        class BasicActor : IAmbientContract
        {
        }


        [StObj( Container = typeof(BasicPackage), ItemKind = DependentItemKind.Item )]
        class BasicGroup : IAmbientContract
        {
            void Construct( BasicActor actor )
            {
            }
        }

        class ZonePackage : BasicPackage
        {
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKind.Item )]
        class ZoneGroup : BasicGroup
        {
            void Construct( SecurityZone zone )
            {
            }
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKind.Item )]
        class SecurityZone : IAmbientContract
        {
            void Construct( BasicGroup group )
            {
            }
        }

        [Test]
        public void LayeredArchitecture()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterClass( typeof( BasicPackage ) );
            collector.RegisterClass( typeof( BasicActor ) );
            collector.RegisterClass( typeof( BasicGroup ) );
            collector.RegisterClass( typeof( ZonePackage ) );
            collector.RegisterClass( typeof( ZoneGroup ) );
            collector.RegisterClass( typeof( SecurityZone ) );
            collector.RegisterClass( typeof( SqlDatabaseDefault ) );           
            collector.DependencySorterHookInput = TestHelper.Trace;
            collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );
            
            var r = collector.GetResult();
            Assert.That( r.HasFatalError, Is.False );
            var basicPackage = r.Default.Find<BasicPackage>();
            var basicActor = r.Default.Find<BasicActor>();
            var basicGroup = r.Default.Find<BasicGroup>();
            var zonePackage = r.Default.Find<ZonePackage>();
            var zoneGroup = r.Default.Find<ZoneGroup>();
            var securityZone = r.Default.Find<SecurityZone>();
            var sqlDatabaseDefault = r.Default.Find<SqlDatabaseDefault>();

            r.Default.CheckChildren<BasicPackage>( "BasicActor,BasicGroup" );
            r.Default.CheckChildren<ZonePackage>( "SecurityZone,ZoneGroup" );
            r.Default.CheckChildren<SqlDatabaseDefault>( "BasicPackage,BasicActor,BasicGroup,ZonePackage,SecurityZone,ZoneGroup" );
        }
    }
}
