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
    public class SimpleObjectsTests
    {
        [Test]
        public void DiscoverSimpleObjects()
        {
            AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
            disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
            disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";
            
            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector();
            collector.RegisterTypes( disco, TestHelper.Logger );
            
            var result = collector.GetResult( TestHelper.Logger );
            Assert.That( result.HasFatalError, Is.False );

        }

        [Test]
        public void DiscoverWithLevel3()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "Without ObjectALevel4 class." ) )
            {
                
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    (t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3")
                    && t.Name != "ObjectALevel4";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.False );
            }

            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectALevel4 class (specializes ObjectALevel3 and use IAbstractionBOnLevel2)." ) )
            {
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.False );
            }
        }

        [Test]
        public void CycleInPackage()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "A specialization of ObjectBLevel3 wants to be in PackageForAB." ) )
            {
                // ↳ PackageForAB ∋ ObjectBLevel3_InPackageForAB ⇒ ObjectBLevel2 ⇒ ObjectBLevel1 ∈ PackageForABLevel1 ⇒ PackageForAB.
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3"
                    || t.Name == "ObjectBLevel3_InPackageForAB";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void Cycle()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectXNeedsY and ObjectYNeedsX." ) )
            {
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    t.Name == "ObjectXNeedsY" || t.Name == "ObjectYNeedsX"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void MissingReference()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "ObjectXNeedsY without ObjectYNeedsX." ) )
            {
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = 
                    t => t.Name == "ObjectXNeedsY"
                    || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );
                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.True );
            }
        }

        [Test]
        public void LoggerInjection()
        {
            using( TestHelper.Logger.OpenGroup( LogLevel.Info, "Logger injection (and optional parameter)." ) )
            {
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t => t.Name == "LoggerInjected";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );
                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.False );

                IStObj theObject = result.Default.StObjMapper[typeof( CK.Setup.StObj.Tests.SimpleObjects.LoggerInjection.LoggerInjected )];
                Assert.That( theObject, Is.Not.Null );
                Assert.That( theObject.StObj, Is.Not.Null.And.InstanceOf<CK.Setup.StObj.Tests.SimpleObjects.LoggerInjection.LoggerInjected>() );
            }
        }

    }
}
