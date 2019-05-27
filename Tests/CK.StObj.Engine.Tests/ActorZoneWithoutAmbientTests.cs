using System;
using CK.Core;
using CK.Setup;
using NUnit.Framework;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
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

        class SqlDatabaseDefault : IAmbientObject
        {
        }

        #region Basic Package

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class BasicPackage : IAmbientObject
        {
        }

        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicActor : IAmbientObject
        {
        }


        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicUser : IAmbientObject
        {
        }


        [StObj( Container = typeof( BasicPackage ), ItemKind = DependentItemKindSpec.Item )]
        class BasicGroup : IAmbientObject
        {
            void StObjConstruct( BasicActor actor )
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
            void StObjConstruct( ISecurityZone zone )
            {
            }
        }

        interface ISecurityZone : IAmbientObject
        {
        }

        [StObj( Container = typeof( ZonePackage ), ItemKind = DependentItemKindSpec.Item )]
        class SecurityZone : ISecurityZone
        {
            void StObjConstruct( BasicGroup group )
            {
            }
        }

        #endregion

        #region Authentication Package

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class AuthenticationPackage : IAmbientObject
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
            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterType( typeof( BasicPackage ) );
            collector.RegisterType( typeof( BasicActor ) );
            collector.RegisterType( typeof( BasicUser ) );
            collector.RegisterType( typeof( BasicGroup ) );
            collector.RegisterType( typeof( ZonePackage ) );
            collector.RegisterType( typeof( ZoneGroup ) );
            collector.RegisterType( typeof( SecurityZone ) );
            collector.RegisterType( typeof( AuthenticationPackage ) );
            collector.RegisterType( typeof( AuthenticationUser ) );
            collector.RegisterType( typeof( SqlDatabaseDefault ) );
            collector.DependencySorterHookInput = items => TestHelper.Monitor.TraceDependentItem( items );
            collector.DependencySorterHookOutput = sortedItems => TestHelper.Monitor.TraceSortedItem( sortedItems, false );
            
            var r = collector.GetResult();
            Assert.That( r.HasFatalError, Is.False );

            r.StObjs.CheckChildren<BasicPackage>( "BasicActor,BasicUser,BasicGroup" );
            r.StObjs.CheckChildren<ZonePackage>( "SecurityZone,ZoneGroup" );
            r.StObjs.CheckChildren<SqlDatabaseDefault>( "BasicPackage,BasicActor,BasicUser,BasicGroup,ZonePackage,SecurityZone,ZoneGroup,AuthenticationPackage,AuthenticationUser" );
        }
    }
}
