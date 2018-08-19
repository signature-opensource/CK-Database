#region Proprietary License
/*----------------------------------------------------------------------------
* This file (Tests\CK.StObj.Engine.Tests\SimpleObjectsTests.cs) is part of CK-Database. 
* Copyright © 2007-2014, Invenietis <http://www.invenietis.com>. All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using CK.StObj.Engine.Tests.SimpleObjects;
using NUnit.Framework;
using System.Linq;
using FluentAssertions;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class SimpleObjectsTests
    {
        public class ObjectALevel1Conflict : ObjectA
        {
        }

        [Test]
        public void StObj_must_have_only_one_specialization_chain()
        {
            var types = TestHelper.Assembly.GetTypes()
                            .Where( t => t.IsClass )
                            .Where( t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" );

            var collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterTypes( types.ToList() );
            collector.RegisterType( typeof( ObjectALevel1Conflict ) );
            var result = collector.GetResult();
            result.HasFatalError.Should().BeTrue();
        }



        [Test]
        public void DiscoverSimpleObjects()
        {
            var types = TestHelper.Assembly.GetTypes()
                            .Where( t => t.IsClass )
                            .Where( t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterTypes( types.ToList() );
            
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );

            IStObjResult oa = result.StObjs.ToStObj( typeof(ObjectA) );
            Assert.That( oa.Container.ObjectType == typeof( PackageForAB ) );
            Assert.That( oa.LeafSpecialization.ObjectType == typeof( ObjectALevel3 ) );

            IStObjResult oa1 = result.StObjs.ToStObj( typeof( ObjectALevel1 ) );
            Assert.That( oa1.Generalization == oa );
            Assert.That( oa1.Container.ObjectType == typeof( PackageForABLevel1 ) );

            IStObjResult oa2 = result.StObjs.ToStObj( typeof( ObjectALevel2 ) );
            Assert.That( oa2.Generalization == oa1 );
            Assert.That( oa2.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );

            IStObjResult oa3 = result.StObjs.ToStObj( typeof( ObjectALevel3 ) );
            Assert.That( oa3.Generalization == oa2 );
            Assert.That( oa3.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );
            Assert.That( oa.RootGeneralization.ObjectType == typeof( ObjectA ) );

        }

        [Test]
        public void DiscoverWithLevel3()
        {
            using( TestHelper.Monitor.OpenInfo( "Without ObjectALevel4 class." ) )
            {
                var types = TestHelper.Assembly.GetTypes()
                                .Where( t => t.IsClass )
                                .Where( t => (t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects"
                                              || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3")
                                             && t.Name != "ObjectALevel4" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );

                var result = collector.GetResult( );
                Assert.That( result.HasFatalError, Is.False );
            }

            using( TestHelper.Monitor.OpenInfo( "ObjectALevel4 class (specializes ObjectALevel3 and use IAbstractionBOnLevel2)." ) )
            {
                var types = TestHelper.Assembly.GetTypes()
                                .Where( t => t.IsClass )
                                .Where( t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects"
                                             || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );

                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.False );
            }
        }

        [Test]
        public void CycleInPackage()
        {
            using( TestHelper.Monitor.OpenInfo( "A specialization of ObjectBLevel3 wants to be in PackageForAB." ) )
            {
                // ↳ PackageForAB ∋ ObjectBLevel3_InPackageForAB ⇒ ObjectBLevel2 ⇒ ObjectBLevel1 ∈ PackageForABLevel1 ⇒ PackageForAB.
                var types = TestHelper.Assembly.GetTypes()
                                .Where( t => t.IsClass )
                                .Where( t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects"
                                             || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3"
                                             || t.Name == "ObjectBLevel3_InPackageForAB" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );

                var result = collector.GetResult(  );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void Cycle()
        {
            using( TestHelper.Monitor.OpenInfo( "ObjectXNeedsY and ObjectYNeedsX." ) )
            {
                var types = TestHelper.Assembly.GetTypes()
                               .Where( t => t.IsClass )
                               .Where( t => t.Name == "ObjectXNeedsY"
                                             || t.Name == "ObjectYNeedsX"
                                             || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );

                var result = collector.GetResult(  );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void MissingReference()
        {
            using( TestHelper.Monitor.OpenInfo( "ObjectXNeedsY without ObjectYNeedsX." ) )
            {
                var types = TestHelper.Assembly.GetTypes()
                               .Where( t => t.IsClass )
                               .Where( t => t.Name == "ObjectXNeedsY"
                                             || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );
                var result = collector.GetResult(  );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void LoggerInjection()
        {
            using( TestHelper.Monitor.OpenInfo( "ConsoleMonitor injection (and optional parameter)." ) )
            {
                var types = TestHelper.Assembly.GetTypes()
                                .Where( t => t.Name == "LoggerInjected" );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
                collector.RegisterTypes( types.ToList() );
                var result = collector.GetResult(  );
                Assert.That( result.HasFatalError, Is.False );

                IStObjResult theObject = result.StObjs.ToLeaf( typeof(CK.StObj.Engine.Tests.SimpleObjects.LoggerInjection.LoggerInjected) );
                Assert.That( theObject, Is.Not.Null );
                Assert.That( theObject.InitialObject, Is.Not.Null.And.InstanceOf<CK.StObj.Engine.Tests.SimpleObjects.LoggerInjection.LoggerInjected>() );
            }
        }

        #region Buggy & Valid Model

        [StObj( ItemKind = DependentItemKindSpec.Container )]
        class C1 : IAmbientContract
        {
        }

        [StObj( Container = typeof( C1 ), ItemKind = DependentItemKindSpec.Container )]
        class C2InC1 : IAmbientContract
        {
        }

        class C3InC2SpecializeC1 : C1
        {
            void StObjConstruct( [Container]C2InC1 c2 )
            {
            }
        }

        [Test]
        public void BuggyModelBecauseOfContainment()
        {
            //Error: Cycle detected: 
            //    ↳ []CK.StObj.Engine.Tests.SimpleObjectsTests+C1 
            //        ⊐ []CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1 
            //            ⊐ []CK.StObj.Engine.Tests.SimpleObjectsTests+C3InC2SpecializeC1 
            //                ↟ []CK.StObj.Engine.Tests.SimpleObjectsTests+C1.

            var types = TestHelper.Assembly.GetTypes()
                            .Where( t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                                         || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                                         || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3InC2SpecializeC1" );


            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterTypes( types.ToList() );
            var result = collector.GetResult( );
            Assert.That( result.HasFatalError, Is.True );
        }

        [StObj( ItemKind = DependentItemKindSpec.Container, Container = typeof( C2InC1 ), Children = new Type[] { typeof( C1 ) } )]
        class C3ContainsC1 : IAmbientContract
        {
        }

        [Test]
        public void BuggyModelBecauseOfContainmentCycle()
        {
            //Error: Cycle detected: 
            //    ↳ []CK.StObj.Engine.Tests.SimpleObjectsTests+C1 
            //        ⊏ []CK.StObj.Engine.Tests.SimpleObjectsTests+C3ContainsC1 
            //            ⊏ []CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1 
            //                ⊏ []CK.StObj.Engine.Tests.SimpleObjectsTests+C1.

            var types = TestHelper.Assembly.GetTypes()
                            .Where( t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                                         || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                                         || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3ContainsC1" );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterTypes( types.ToList() );
            var result = collector.GetResult(  );
            Assert.That( result.HasFatalError, Is.True );
        }

        class C3RequiresC2SpecializeC1 : C1
        {
            void StObjConstruct( C2InC1 c2 )
            {
            }
        }

        [Test]
        public void ValidModelWithRequires()
        {
            var types = TestHelper.Assembly.GetTypes()
                           .Where( t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                                        || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                                        || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3RequiresC2SpecializeC1" );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor, new SimpleServiceContainer() );
            collector.RegisterTypes( types.ToList() );
            var result = collector.GetResult(  );
            Assert.That( result.HasFatalError, Is.False );
        
        }
        #endregion

    }
}
