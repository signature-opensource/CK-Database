using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CK.Core;
using System.Reflection;

namespace CK.Setup.StObj.Tests
{
    [TestFixture]
    public class SimpleObjectsTests
    {
        public void DiscoverSimpleObjects()
        {
            AssemblyDiscoverer disco = new AssemblyDiscoverer( TestHelper.Logger );
            disco.TypeFilter = t => t.Namespace == "CK.Setup.StObj.Tests.SimpleObjects";   
            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector();
            collector.RegisterTypes( disco, TestHelper.Logger );
        }
    }
}
