using System;
using System.Reflection;
using CK.Core;
using CK.Setup;
using NUnit.Framework;
using CK.StObj.Engine.Tests.Poco;
using System.Linq;

namespace CK.StObj.Engine.Tests
{
    [TestFixture]
    public class PocoTests
    {
        [Test]
        public void simple_poco_resolution_and_injection()
        {
            StObjCollectorResult result = BuildPocoSample();

            IStObjResult p = result.Default.StObjMap.ToStObj( typeof( PackageWithBasicPoco ) );
            var package = (PackageWithBasicPoco)p.InitialObject;
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

        static StObjCollectorResult BuildPocoSample()
        {
            var types = TestHelper.Assembly.GetTypes()
                            .Where( t => t.Namespace == "CK.StObj.Engine.Tests.Poco" );

            StObjCollector collector = new StObjCollector( TestHelper.Monitor );
            collector.RegisterTypes( types.ToList() );

            var result = collector.GetResult( new SimpleServiceContainer() );
            Assert.That( result.HasFatalError, Is.False );
            return result;
        }

        [Test]
        public void poco_factory_exposes_the_final_type()
        {
            StObjCollectorResult result = BuildPocoSample();
            var p = result.Default.StObjMap.Obtain<IPocoFactory<IBasicPoco>>();

            Type pocoType = p.PocoClassType;
            Assert.That( typeof( IBasicPoco ).IsAssignableFrom( pocoType ) );
            Assert.That( typeof( IEAlternateBasicPoco ).IsAssignableFrom( pocoType ) );
            Assert.That( typeof( IEBasicPoco ).IsAssignableFrom( pocoType ) );
            Assert.That( typeof( IECombineBasicPoco ).IsAssignableFrom( pocoType ) );
            Assert.That( typeof( IEIndependentBasicPoco ).IsAssignableFrom( pocoType ) );

        }

        [Test]
        public void poco_support_read_only_properties()
        {
            StObjCollectorResult result = BuildPocoSample();
            var p = result.Default.StObjMap.Obtain<IPocoFactory<IEBasicPocoWithReadOnly>>();
            var o = p.Create();

            Assert.That( o.ReadOnlyProperty, Is.EqualTo( 0 ) );
            p.PocoClassType.GetProperty( nameof(IEBasicPocoWithReadOnly.ReadOnlyProperty) )
                .SetValue( o, 3712 );
            Assert.That( o.ReadOnlyProperty, Is.EqualTo( 3712 ) );
        }


    }
}
