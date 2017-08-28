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

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class SimpleObjectsTests
    {
        [Test]
        public void DiscoverSimpleObjects()
        {
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
            disco.AssemblyFilter = a => a == TestHelper.Assembly;
            disco.TypeFilter = t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects";
            
            disco.DiscoverRecurse( TestHelper.Assembly );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( disco );
            
            var result = collector.GetResult( new SimpleServiceContainer() );
            Assert.That( result.HasFatalError, Is.False );

            IStObjResult oa = result.Default.StObjMap.ToStObj( typeof(ObjectA) );
            Assert.That( oa.Container.ObjectType == typeof( PackageForAB ) );
            Assert.That( oa.LeafSpecialization.ObjectType == typeof( ObjectALevel3 ) );

            IStObjResult oa1 = result.Default.StObjMap.ToStObj( typeof( ObjectALevel1 ) );
            Assert.That( oa1.Generalization == oa );
            Assert.That( oa1.Container.ObjectType == typeof( PackageForABLevel1 ) );

            IStObjResult oa2 = result.Default.StObjMap.ToStObj( typeof( ObjectALevel2 ) );
            Assert.That( oa2.Generalization == oa1 );
            Assert.That( oa2.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );

            IStObjResult oa3 = result.Default.StObjMap.ToStObj( typeof( ObjectALevel3 ) );
            Assert.That( oa3.Generalization == oa2 );
            Assert.That( oa3.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );
            Assert.That( oa.RootGeneralization.ObjectType == typeof( ObjectA ) );

        }

        [Test]
        public void DiscoverWithLevel3()
        {
            using( TestHelper.Monitor.OpenInfo().Send( "Without ObjectALevel4 class." ) )
            {
                
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );

                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = t =>
                    (t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3")
                    && t.Name != "ObjectALevel4";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );

                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.False );
            }

            using( TestHelper.Monitor.OpenInfo().Send( "ObjectALevel4 class (specializes ObjectALevel3 and use IAbstractionBOnLevel2)." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );

                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = t => t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects" || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );

                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.False );
            }
        }

        [Test]
        public void CycleInPackage()
        {
            using( TestHelper.Monitor.OpenInfo().Send( "A specialization of ObjectBLevel3 wants to be in PackageForAB." ) )
            {
                // ↳ PackageForAB ∋ ObjectBLevel3_InPackageForAB ⇒ ObjectBLevel2 ⇒ ObjectBLevel1 ∈ PackageForABLevel1 ⇒ PackageForAB.
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = t =>
                    t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects"
                    || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects.WithLevel3"
                    || t.Name == "ObjectBLevel3_InPackageForAB";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );

                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void Cycle()
        {
            using( TestHelper.Monitor.OpenInfo().Send( "ObjectXNeedsY and ObjectYNeedsX." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = t =>
                    t.Name == "ObjectXNeedsY" || t.Name == "ObjectYNeedsX"
                    || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );

                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void MissingReference()
        {
            using( TestHelper.Monitor.OpenInfo().Send( "ObjectXNeedsY without ObjectYNeedsX." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = 
                    t => t.Name == "ObjectXNeedsY"
                    || t.Namespace == "CK.StObj.Engine.Tests.SimpleObjects";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );
                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void LoggerInjection()
        {
            using( TestHelper.Monitor.OpenInfo().Send( "ConsoleMonitor injection (and optional parameter)." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
                disco.AssemblyFilter = a => a == TestHelper.Assembly;
                disco.TypeFilter = t => t.Name == "LoggerInjected";

                disco.DiscoverRecurse( TestHelper.Assembly );

                StObjCollector collector = new StObjCollector( TestHelper.Monitor );
                collector.RegisterTypes( disco );
                var result = collector.GetResult( new SimpleServiceContainer() );
                Assert.That( result.HasFatalError, Is.False );

                IStObjResult theObject = result.Default.StObjMap.ToLeaf( typeof(CK.StObj.Engine.Tests.SimpleObjects.LoggerInjection.LoggerInjected) );
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
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
            disco.TypeFilter =
                t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3InC2SpecializeC1";

            disco.Discover( TestHelper.Assembly );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( disco );
            var result = collector.GetResult( new SimpleServiceContainer() );
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
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
            disco.TypeFilter =
                t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3ContainsC1";

            disco.Discover( TestHelper.Assembly );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( disco );
            var result = collector.GetResult( new SimpleServiceContainer() );
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
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
            disco.TypeFilter =
                t => t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.StObj.Engine.Tests.SimpleObjectsTests+C3RequiresC2SpecializeC1";

            disco.Discover( TestHelper.Assembly );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( disco );
            var result = collector.GetResult( new SimpleServiceContainer() );
            Assert.That( result.HasFatalError, Is.False );
        
        }
        #endregion

    }
}
