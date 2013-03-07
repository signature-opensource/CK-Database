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
    public class ActorZoneWithAmbientTests
    {
        [StObj( ItemKind = DependentItemKind.Group, TrackAmbientProperties = TrackAmbientPropertiesMode.AddPropertyHolderAsChildren )] 
        class SqlDatabaseDefault : IAmbientContract
        {
        }

        class BaseDatabaseObject : IAmbientContractDefiner
        {
            [AmbientProperty]
            public SqlDatabaseDefault Database { get; set; }
        }

        #region Basic Package

        [StObj( ItemKind = DependentItemKind.Container )]
        class BasicPackage : BaseDatabaseObject
        {
            [AmbientContract]
            public BasicUser UserHome { get; protected set; }
            
            [AmbientContract]
            public BasicGroup GroupHome { get; protected set; }
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKind.Item )]
        class BasicActor : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKind.Item )]
        class BasicUser : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKind.Item )]
        class BasicGroup : BaseDatabaseObject
        {
            void Construct( BasicActor actor )
            {
            }
        }

        #endregion

        #region Zone Package

        class ZonePackage : BasicPackage
        {
            [AmbientContract]
            public new ZoneGroup GroupHome { get { return (ZoneGroup)base.GroupHome; } protected set { base.GroupHome = value; } }
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKind.Item )]
        class ZoneGroup : BasicGroup
        {
            void Construct( SecurityZone zone )
            {
            }
        }
        
        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKind.Item )]
        class SecurityZone : BaseDatabaseObject
        {
            void Construct( BasicGroup group )
            {
            }
        }

        #endregion

        #region Authentication Package

        [StObj( ItemKind = DependentItemKind.Container )]
        class AuthenticationPackage : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( AuthenticationPackage ) )]
        class AuthenticationUser : BasicUser
        {
        }

        #endregion

        [Test]
        public void LayeredArchitecture()
        {
            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterClass( typeof( BasicPackage ) );
            collector.RegisterClass( typeof( BasicActor ) );
            collector.RegisterClass( typeof( BasicUser ) );
            collector.RegisterClass( typeof( BasicGroup ) );
            collector.RegisterClass( typeof( ZonePackage ) );
            collector.RegisterClass( typeof( ZoneGroup ) );
            collector.RegisterClass( typeof( SecurityZone ) );
            collector.RegisterClass( typeof( AuthenticationPackage ) );
            collector.RegisterClass( typeof( AuthenticationUser ) );
            collector.RegisterClass( typeof( SqlDatabaseDefault ) );
            collector.DependencySorterHookInput = TestHelper.Trace;
            collector.DependencySorterHookOutput = sortedItems => TestHelper.Trace( sortedItems, false );

            var r = collector.GetResult();
            Assert.That( r.HasFatalError, Is.False );
            r.Default.CheckChildren<BasicPackage>( "BasicActor,BasicUser,BasicGroup" );
            r.Default.CheckChildren<ZonePackage>( "SecurityZone,ZoneGroup" );
            r.Default.CheckChildren<SqlDatabaseDefault>( "BasicPackage,BasicActor,BasicUser,BasicGroup,ZonePackage,SecurityZone,ZoneGroup,AuthenticationPackage,AuthenticationUser" );

            var basicPackage = r.Default.StObjMapper.GetObject<BasicPackage>();
            Assert.That( basicPackage is ZonePackage );
            Assert.That( basicPackage.GroupHome is ZoneGroup );

        }
    }
}
