using System;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    [CLSCompliant(false)]
    public class ActorZoneWithoutAmbientTests
    {
        [StObj( ItemKind = DependentItemKindSpec.Group,
                Children = new Type[] 
                { 
                    typeof( BasicPackage ), 
                    typeof( BasicActor ), 
                    typeof( BasicUser ), 
                    typeof( BasicGroup ), 
                    typeof( ZonePackage ), 
                    typeof( ZoneGroup ), 
                    typeof( SecurityZone ),
                    typeof( AuthenticationPackage ),
                    typeof( AuthenticationUser )
                } )]

        class SqlDatabaseDefault : IAmbientContract
        {
        }

        #region Basic Package

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class BasicPackage : IAmbientContract
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicActor : IAmbientContract
        {
        }


        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicUser : IAmbientContract
        {
        }


        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicGroup : IAmbientContract
        {
            void Construct( BasicActor actor )
            {
            }
        }

        #endregion

        #region Zone Package

        class ZonePackage : BasicPackage
        {
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKindSpec.Item )]
        class ZoneGroup : BasicGroup
        {
            void Construct( SecurityZone zone )
            {
            }
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKindSpec.Item )]
        class SecurityZone : IAmbientContract
        {
            void Construct( BasicGroup group )
            {
            }
        }

        #endregion

        #region Authentication Package

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class AuthenticationPackage : IAmbientContract
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
            var basicPackage = r.Default.FindStObj<BasicPackage>();
            var basicActor = r.Default.FindStObj<BasicActor>();
            var basicGroup = r.Default.FindStObj<BasicGroup>();
            var zonePackage = r.Default.FindStObj<ZonePackage>();
            var zoneGroup = r.Default.FindStObj<ZoneGroup>();
            var securityZone = r.Default.FindStObj<SecurityZone>();
            var sqlDatabaseDefault = r.Default.FindStObj<SqlDatabaseDefault>();

            r.Default.CheckChildren<BasicPackage>( "BasicActor,BasicUser,BasicGroup" );
            r.Default.CheckChildren<ZonePackage>( "SecurityZone,ZoneGroup" );
            r.Default.CheckChildren<SqlDatabaseDefault>( "BasicPackage,BasicActor,BasicUser,BasicGroup,ZonePackage,SecurityZone,ZoneGroup,AuthenticationPackage,AuthenticationUser" );
        }
    }
}
