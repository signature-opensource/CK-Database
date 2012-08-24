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
            
            SimpleObjectsTrace.Log.GetStringBuilder().Clear();
            
            var result = collector.GetResult( TestHelper.Logger );
            Assert.That( result.HasFatalError, Is.False );

        }

        [Test]
        public void DiscoverWithLevel3()
        {
            {
                // Without ObjectALevel4
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t =>
                    (t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3")
                    && t.Name != "ObjectALevel4";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                SimpleObjectsTrace.Log.GetStringBuilder().Clear();

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.False );

                Console.WriteLine( SimpleObjectsTrace.Log.GetStringBuilder().ToString() );
            }

            {
                // With ObjectALevel4
                AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );

                disco.AssemblyFilter = a => a == Assembly.GetExecutingAssembly();
                disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects" || t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects.WithLevel3";

                disco.Discover( Assembly.GetExecutingAssembly() );

                StObjCollector collector = new StObjCollector();
                collector.RegisterTypes( disco, TestHelper.Logger );

                SimpleObjectsTrace.Log.GetStringBuilder().Clear();

                var result = collector.GetResult( TestHelper.Logger );
                Assert.That( result.HasFatalError, Is.False );
                
                Console.WriteLine( SimpleObjectsTrace.Log.GetStringBuilder().ToString() );
            }

        }
    }
}
