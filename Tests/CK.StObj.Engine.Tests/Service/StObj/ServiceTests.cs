using CK.Core;
using CK.Setup;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.StObj.Engine.Tests.Service.StObj
{
    namespace Local
    {
        [AttributeUsage( AttributeTargets.Class, AllowMultiple = true, Inherited = false )]
        class ReplaceAmbientServiceAttribute : Attribute
        {
            public ReplaceAmbientServiceAttribute( string replacedAssemblyQualifiedName )
            {
            }
        }
    }

    [TestFixture]
    public class ServiceTests : TestsBase
    {
        interface ISampleService : IAmbientService
        {
        }

        class SampleService : ISampleService
        {
        }

        [ReplaceAmbientService(typeof(SampleService))]
        class SampleService2 : ISampleService
        {
        }


        [Test]
        public void ReplaceAmbientService_works_with_type()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SampleService ) );
            collector.RegisterType( typeof( SampleService2 ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISampleService )].ClassType.Should().Be( typeof( SampleService2 ) );
            r.Services.SimpleMappings[typeof( SampleService )].ClassType.Should().Be( typeof( SampleService ) );
        }
#if NET461
        [Local.ReplaceAmbientService( "CK.StObj.Engine.Tests.Service.StObj.ServiceTests+SampleService2, CK.StObj.Engine.Tests" )]
#else
        [Local.ReplaceAmbientService( "CK.StObj.Engine.Tests.Service.StObj.ServiceTests+SampleService2, CK.StObj.Engine.NetCore.Tests" )]
#endif
        class SampleService3 : ISampleService
        {
        }

        [Test]
        public void ReplaceAmbientService_works_with_assembly_qualified_name_and_locally_defined_attribute()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( SampleService ) );
            collector.RegisterType( typeof( SampleService2 ) );
            collector.RegisterType( typeof( SampleService3 ) );
            var r = CheckSuccess( collector );
            r.Services.SimpleMappings[typeof( ISampleService )].ClassType.Should().Be( typeof( SampleService3 ) );
            r.Services.SimpleMappings[typeof( SampleService2 )].ClassType.Should().Be( typeof( SampleService2 ) );
            r.Services.SimpleMappings[typeof( SampleService )].ClassType.Should().Be( typeof( SampleService ) );
        }


        class UseActivityMonitor : ISingletonAmbientService
        {
            public UseActivityMonitor( IActivityMonitor m )
            {
            }
        }

        [Test]
        public void IActivityMonitor_is_Scoped_by_default()
        {
            var collector = CreateStObjCollector();
            collector.RegisterType( typeof( UseActivityMonitor ) );
            CheckFailure( collector );
        }

    }
}
