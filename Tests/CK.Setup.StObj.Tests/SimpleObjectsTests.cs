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
    [CLSCompliant( false )]
    [TestFixture]
    public class SimpleObjectsTests
    {
        [Test]
        public void DiscoverSimpleObjects()
        {
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
            disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
            disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";
            
            disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterTypes( disco );
            
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );

            IStObj oa = result.Default.StObjMapper.Find<ObjectA>();
            Assert.That( oa.Container.ObjectType == typeof( PackageForAB ) );
            Assert.That( oa.LeafSpecialization.ObjectType == typeof( ObjectALevel3 ) );
            
            IStObj oa1 = result.Default.StObjMapper.Find<ObjectALevel1>();
            Assert.That( oa1.Generalization == oa );
            Assert.That( oa1.Container.ObjectType == typeof( PackageForABLevel1 ) );
            
            IStObj oa2 = result.Default.StObjMapper.Find<ObjectALevel2>();
            Assert.That( oa2.Generalization == oa1 );
            Assert.That( oa2.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );

            IStObj oa3 = result.Default.StObjMapper.Find<ObjectALevel3>();
            Assert.That( oa3.Generalization == oa2 );
            Assert.That( oa3.Container.ObjectType == typeof( PackageForABLevel1 ), "Inherited." );
            Assert.That( oa.RootGeneralization.ObjectType == typeof( ObjectA ) );

        }

        [Test]
        public void DiscoverWithLevel3()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "Without ObjectALevel4 class." ) )
            {
                
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    (t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3")
                    && t.Name != "ObjectALevel4";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );

                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.False );
            }

            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectALevel4 class (specializes ObjectALevel3 and use IAbstractionBOnLevel2)." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );

                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.False );
            }
        }

        [Test]
        public void CycleInPackage()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "A specialization of ObjectBLevel3 wants to be in PackageForAB." ) )
            {
                // ↳ PackageForAB ∋ ObjectBLevel3_InPackageForAB ⇒ ObjectBLevel2 ⇒ ObjectBLevel1 ∈ PackageForABLevel1 ⇒ PackageForAB.
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3"
                    || t.Name == "ObjectBLevel3_InPackageForAB";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );

                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void Cycle()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectXNeedsY and ObjectYNeedsX." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    t.Name == "ObjectXNeedsY" || t.Name == "ObjectYNeedsX"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );

                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void MissingReference()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectXNeedsY without ObjectYNeedsX." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = 
                    t => t.Name == "ObjectXNeedsY"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );
                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void LoggerInjection()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "Logger injection (and optional parameter)." ) )
            {
                AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t => t.Name == "LoggerInjected";

                disco.DiscoverRecurse( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector( TestHelper.Logger );
                collector.RegisterTypes( disco );
                var result = collector.GetResult();
                Assert.That( result.HasFatalError, Is.False );

                IStObj theObject = result.Default.StObjMapper.Find<CK.Setup.StObj.Tests.SimpleObjects.LoggerInjection.LoggerInjected>();
                Assert.That( theObject, Is.Not.Null );
                Assert.That( theObject.Object, Is.Not.Null.And.InstanceOf<CK.Setup.StObj.Tests.SimpleObjects.LoggerInjection.LoggerInjected>() );
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
            void Construct( [Container]C2InC1 c2 )
            {
            }
        }

        [Test]
        public void BuggyModelBecauseOfContainment()
        {
            //Error: Cycle detected: 
            //    ↳ []CK.Setup.StObj.Tests.SimpleObjectsTests+C1 
            //        ⊐ []CK.Setup.StObj.Tests.SimpleObjectsTests+C2InC1 
            //            ⊐ []CK.Setup.StObj.Tests.SimpleObjectsTests+C3InC2SpecializeC1 
            //                ↟ []CK.Setup.StObj.Tests.SimpleObjectsTests+C1.
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
            disco.TypeFilter =
                t => t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C3InC2SpecializeC1";

            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterTypes( disco );
            var result = collector.GetResult();
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
            //    ↳ []CK.Setup.StObj.Tests.SimpleObjectsTests+C1 
            //        ⊏ []CK.Setup.StObj.Tests.SimpleObjectsTests+C3ContainsC1 
            //            ⊏ []CK.Setup.StObj.Tests.SimpleObjectsTests+C2InC1 
            //                ⊏ []CK.Setup.StObj.Tests.SimpleObjectsTests+C1.
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
            disco.TypeFilter =
                t => t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C3ContainsC1";

            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterTypes( disco );
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.True );

        }

        class C3RequiresC2SpecializeC1 : C1
        {
            void Construct( C2InC1 c2 )
            {
            }
        }

        [Test]
        public void ValidModelWithRequires()
        {
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Logger );
            disco.TypeFilter =
                t => t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C2InC1"
                || t.FullName == "CK.Setup.StObj.Tests.SimpleObjectsTests+C3RequiresC2SpecializeC1";

            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector( TestHelper.Logger );
            collector.RegisterTypes( disco );
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );
        }
        #endregion

    }
}
