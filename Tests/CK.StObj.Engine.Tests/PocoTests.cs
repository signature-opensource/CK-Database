using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using CK.StObj.Engine.Tests.Poco;

namespace CK.StObj.Engine.Tests
{
    [CLSCompliant( false )]
    [TestFixture]
    public class PocoTests
    {
        [Test]
        public void simple_poco_resolution_and_injection()
        {
            AssemblyRegisterer disco = new AssemblyRegisterer( TestHelper.Monitor );
            disco.TypeFilter = t => t.Namespace == "CK.StObj.Engine.Tests.Poco";
            disco.Discover( Assembly.GetExecutingAssembly() );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( disco );
            
            var result = collector.GetResult();
            Assert.That( result.HasFatalError, Is.False );

            IStObjResult p = result.Default.StObjMap.ToStObj( typeof( PackageWithBasicPoco ) );
            var package = (PackageWithBasicPoco)p.ObjectAccessor();
            IBasicPoco poco = package.Factory.Create();
            Assert.That( poco is IEAlternateBasicPoco );
            Assert.That( poco is IEBasicPoco );
            Assert.That( poco is IECombineBasicPoco );
            Assert.That( poco is IEIndependentBasicPoco );

            var fEI = result.Default.StObjMap.Obtain<IPocoFactory<IEIndependentBasicPoco>>();
            IEIndependentBasicPoco ei = fEI.Create();
            ei.BasicProperty = 3;
            ei.IndependentProperty = 9;
        }

    }
}
