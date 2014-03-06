using System;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    [CLSCompliant(false)]
    public class ActorZoneWithAmbientTests
    {
        public class AmbientPropertySetAttribute : Attribute, IStObjStructuralConfigurator
        {
            public string PropertyName { get; set; }

            public object PropertyValue { get; set; }

            public void Configure( IActivityMonitor monitor, IStObjMutableItem o )
            {
                o.SetAmbiantPropertyValue( monitor, PropertyName, PropertyValue, "AmbientPropertySetAttribute" );
            }
        }


        [StObj( ItemKind = DependentItemKindSpec.Group, TrackAmbientProperties = TrackAmbientPropertiesMode.AddPropertyHolderAsChildren )] 
        class SqlDatabaseDefault : IAmbientContract
        {
        }

        class BaseDatabaseObject : IAmbientContractDefiner
        {
            [AmbientProperty]
            public SqlDatabaseDefault Database { get; set; }
            
            [AmbientProperty]
            public string Schema { get; set; }
        }

        #region Basic Package

        // We want BasicActor, BasicUser and BasicGroup to be in CK schema since they belong to BasicPackage.
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        [AmbientPropertySet( PropertyName = "Schema", PropertyValue = "CK" )]
        class BasicPackage : BaseDatabaseObject
        {
            [InjectContract]
            public BasicUser UserHome { get; protected set; }
            
            [InjectContract]
            public BasicGroup GroupHome { get; protected set; }
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicActor : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicUser : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicGroup : BaseDatabaseObject
        {
            void Construct( BasicActor actor )
            {
            }
        }

        #endregion

        #region Zone Package

        // ZonePackage specializes BasicPackage. Its Schema is the same as BasicPackage (CK).
        class ZonePackage : BasicPackage
        {
            [InjectContract]
            public new ZoneGroup GroupHome { get { return (ZoneGroup)base.GroupHome; } }
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKindSpec.Item )]
        class ZoneGroup : BasicGroup
        {
            void Construct( SecurityZone zone )
            {
            }
        }

        // This new object in ZonePackage will be in CK schema.
        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKindSpec.Item )]
        class SecurityZone : BaseDatabaseObject
        {
            void Construct( BasicGroup group )
            {
            }
        }

        #endregion

        #region Authentication Package

        // This new Package introduces a new Schema: CKAuth.
        // The objects that are specializations of objects from other packages must stay in CK.
        // But a new object like AuthenticationDetail must be in CKAuth.
        [StObj( ItemKind = DependentItemKindSpec.Container )]
        [AmbientPropertySet( PropertyName = "Schema", PropertyValue = "CKAuth" )] 
        class AuthenticationPackage : BaseDatabaseObject
        {
        }

        [StObj( Container = typeof( AuthenticationPackage ) )]
        class AuthenticationUser : BasicUser
        {
        }

        [StObj( Container = typeof( AuthenticationPackage ) )]
        class AuthenticationDetail : BaseDatabaseObject
        {
        }

        #endregion

        [Test]
        public void LayeredArchitecture()
        {
            StObjCollector collector = new StObjCollector( TestHelper.ConsoleMonitor );
            collector.RegisterClass( typeof( BasicPackage ) );
            collector.RegisterClass( typeof( BasicActor ) );
            collector.RegisterClass( typeof( BasicUser ) );
            collector.RegisterClass( typeof( BasicGroup ) );
            collector.RegisterClass( typeof( ZonePackage ) );
            collector.RegisterClass( typeof( ZoneGroup ) );
            collector.RegisterClass( typeof( SecurityZone ) );
            collector.RegisterClass( typeof( AuthenticationPackage ) );
            collector.RegisterClass( typeof( AuthenticationUser ) );
            collector.RegisterClass( typeof( AuthenticationDetail ) );
            collector.RegisterClass( typeof( SqlDatabaseDefault ) );
            collector.DependencySorterHookInput = items => TestHelper.ConsoleMonitor.TraceDependentItem( items );
            collector.DependencySorterHookOutput = sortedItems => TestHelper.ConsoleMonitor.TraceSortedItem( sortedItems, false );


            var r = collector.GetResult();
            Assert.That( r.HasFatalError, Is.False );
            r.Default.CheckChildren<BasicPackage>( "BasicActor,BasicUser,BasicGroup" );
            r.Default.CheckChildren<ZonePackage>( "SecurityZone,ZoneGroup" );
            r.Default.CheckChildren<SqlDatabaseDefault>( "BasicPackage,BasicActor,BasicUser,BasicGroup,ZonePackage,SecurityZone,ZoneGroup,AuthenticationPackage,AuthenticationUser,AuthenticationDetail" );

            var basicPackage = r.Default.StObjMap.Obtain<BasicPackage>();
            Assert.That( basicPackage is ZonePackage );
            Assert.That( basicPackage.GroupHome is ZoneGroup );
            Assert.That( basicPackage.Schema, Is.EqualTo( "CK" ) );

            var authenticationUser = r.Default.StObjMap.Obtain<AuthenticationUser>();
            Assert.That( authenticationUser.Schema, Is.EqualTo( "CK" ) );
            
            var authenticationDetail = r.Default.StObjMap.Obtain<AuthenticationDetail>();
            Assert.That( authenticationDetail.Schema, Is.EqualTo( "CKAuth" ) );
        }
    }
}
